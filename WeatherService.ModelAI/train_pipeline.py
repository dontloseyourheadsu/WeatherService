import json
import logging
import math
import os
from dataclasses import dataclass
from pathlib import Path
from typing import Tuple

import numpy as np
import pandas as pd
import torch
import torch.nn as nn
import torch.optim as optim
from pymongo import MongoClient
from sklearn.cluster import MiniBatchKMeans
from sklearn.preprocessing import StandardScaler
from torch.utils.data import DataLoader, TensorDataset

"""Model training pipeline for spatiotemporal weather forecasting.

The pipeline streams observations from MongoDB in batches, aggregates them by
hour and spatial cluster, and trains a graph-temporal model that predicts the
next-hour conditions for each cluster.
"""

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")


@dataclass
class Config:
    """Configuration for MongoDB access and training hyperparameters."""
    mongo_uri: str = os.getenv("MONGO_URI", "mongodb://mongo:27017")
    mongo_db: str = os.getenv("MONGO_DB", "WeatherDb")
    mongo_collection: str = os.getenv("MONGO_COLLECTION", "Forecasts")
    max_records: int = int(os.getenv("MAX_RECORDS", "0"))
    start_date: str = os.getenv("START_DATE", "")
    end_date: str = os.getenv("END_DATE", "")
    num_clusters: int = int(os.getenv("NUM_CLUSTERS", "64"))
    cluster_sample_size: int = int(os.getenv("CLUSTER_SAMPLE_SIZE", "200000"))
    mongo_batch_size: int = int(os.getenv("MONGO_BATCH_SIZE", "50000"))
    k_neighbors: int = int(os.getenv("K_NEIGHBORS", "6"))
    input_window: int = int(os.getenv("INPUT_WINDOW", "24"))
    train_split: float = float(os.getenv("TRAIN_SPLIT", "0.8"))
    batch_size: int = int(os.getenv("BATCH_SIZE", "32"))
    epochs: int = int(os.getenv("EPOCHS", "20"))
    learning_rate: float = float(os.getenv("LEARNING_RATE", "1e-3"))
    device: str = os.getenv("DEVICE", "cuda" if torch.cuda.is_available() else "cpu")
    artifacts_dir: Path = Path(os.getenv("ARTIFACTS_DIR", "/app/artifacts"))


class GraphTemporalModel(nn.Module):
    """Graph-then-GRU model for spatiotemporal forecasting."""
    def __init__(self, in_features: int, hidden_features: int, out_features: int, adjacency: torch.Tensor):
        super().__init__()
        self.adjacency = adjacency
        self.graph_linear = nn.Linear(in_features, hidden_features)
        self.gru = nn.GRU(hidden_features, hidden_features, batch_first=True)
        self.output = nn.Linear(hidden_features, out_features)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        """Forward pass for batched sequences."""
        # x: (batch, time, nodes, features)
        batch_size, seq_len, num_nodes, _ = x.shape
        graph_features = []
        for t in range(seq_len):
            x_t = x[:, t]  # (batch, nodes, features)
            x_agg = torch.einsum("ij,bjf->bif", self.adjacency, x_t)
            x_gc = torch.relu(self.graph_linear(x_agg))
            graph_features.append(x_gc)
        graph_features = torch.stack(graph_features, dim=1)  # (batch, time, nodes, hidden)
        graph_features = graph_features.view(batch_size * num_nodes, seq_len, -1)
        _, hidden = self.gru(graph_features)
        hidden = hidden[-1]  # (batch*nodes, hidden)
        output = self.output(hidden)
        output = output.view(batch_size, num_nodes, -1)
        return output


def _build_time_query(cfg: Config) -> dict:
    """Build a MongoDB query for optional time filters."""
    query = {}
    if cfg.start_date or cfg.end_date:
        query["timestamp"] = {}
        if cfg.start_date:
            query["timestamp"]["$gte"] = pd.to_datetime(cfg.start_date)
        if cfg.end_date:
            query["timestamp"]["$lte"] = pd.to_datetime(cfg.end_date)
    return query


def fit_clusters(cfg: Config) -> MiniBatchKMeans:
    """Fit spatial clusters on a random sample of coordinates."""
    logging.info("Sampling coordinates for clustering")
    query = _build_time_query(cfg)

    with MongoClient(cfg.mongo_uri) as client:
        collection = client[cfg.mongo_db][cfg.mongo_collection]
        total_docs = collection.count_documents(query)
        if total_docs == 0:
            raise RuntimeError("No records found for clustering.")

        sample_size = min(cfg.cluster_sample_size, total_docs)
        pipeline = [
            {"$match": query} if query else {"$match": {}},
            {"$sample": {"size": int(sample_size)}},
            {"$project": {"_id": 0, "latitude": 1, "longitude": 1}},
        ]
        sample_docs = list(collection.aggregate(pipeline))

    coords = pd.DataFrame(sample_docs)[["latitude", "longitude"]].dropna().to_numpy()
    if len(coords) == 0:
        raise RuntimeError("No coordinate samples available for clustering.")

    kmeans = MiniBatchKMeans(n_clusters=cfg.num_clusters, random_state=42, batch_size=2048)
    kmeans.fit(coords)
    return kmeans


def stream_aggregate_clusters(cfg: Config, kmeans: MiniBatchKMeans) -> Tuple[np.ndarray, pd.DatetimeIndex]:
    """Stream records in batches and aggregate to hourly cluster features."""
    logging.info("Streaming records and aggregating by hour/cluster")
    query = _build_time_query(cfg)
    projection = {
        "_id": 0,
        "timestamp": 1,
        "latitude": 1,
        "longitude": 1,
        "temperature": 1,
        "windSpeed": 1,
        "windDirection": 1,
    }

    aggregation = {}
    min_time = None
    max_time = None

    def update_aggregation(batch: list[dict]) -> None:
        nonlocal min_time, max_time
        if not batch:
            return
        df = pd.DataFrame(batch)
        if df.empty:
            return
        df["timestamp"] = pd.to_datetime(df["timestamp"], utc=True)
        df = df.dropna(subset=["latitude", "longitude", "temperature", "windSpeed", "windDirection"])
        if df.empty:
            return

        coords = df[["latitude", "longitude"]].to_numpy()
        df["cluster"] = kmeans.predict(coords)
        df["time"] = df["timestamp"].dt.floor("h")
        wind_rad = np.deg2rad(df["windDirection"].astype(float))
        df["windDirSin"] = np.sin(wind_rad)
        df["windDirCos"] = np.cos(wind_rad)

        min_batch_time = df["time"].min()
        max_batch_time = df["time"].max()
        min_time = min_batch_time if min_time is None else min(min_time, min_batch_time)
        max_time = max_batch_time if max_time is None else max(max_time, max_batch_time)

        grouped = (
            df.groupby(["time", "cluster"], observed=True)
            .agg(
                temperature_sum=("temperature", "sum"),
                windSpeed_sum=("windSpeed", "sum"),
                windDirSin_sum=("windDirSin", "sum"),
                windDirCos_sum=("windDirCos", "sum"),
                count=("temperature", "count"),
            )
            .reset_index()
        )

        for row in grouped.itertuples(index=False):
            key = (row.time, int(row.cluster))
            if key not in aggregation:
                aggregation[key] = [0.0, 0.0, 0.0, 0.0, 0]
            aggregation[key][0] += float(row.temperature_sum)
            aggregation[key][1] += float(row.windSpeed_sum)
            aggregation[key][2] += float(row.windDirSin_sum)
            aggregation[key][3] += float(row.windDirCos_sum)
            aggregation[key][4] += int(row.count)

    with MongoClient(cfg.mongo_uri) as client:
        collection = client[cfg.mongo_db][cfg.mongo_collection]
        cursor = collection.find(query, projection, no_cursor_timeout=True).batch_size(cfg.mongo_batch_size)
        if cfg.max_records > 0:
            cursor = cursor.limit(cfg.max_records)

        batch = []
        for doc in cursor:
            batch.append(doc)
            if len(batch) >= cfg.mongo_batch_size:
                update_aggregation(batch)
                batch = []
        update_aggregation(batch)

    if min_time is None or max_time is None:
        raise RuntimeError("No records found for training.")

    time_index = pd.date_range(min_time, max_time, freq="H", tz="UTC")
    num_clusters = cfg.num_clusters
    feature_arrays = {
        "temperature": np.full((len(time_index), num_clusters), np.nan, dtype=np.float32),
        "windSpeed": np.full((len(time_index), num_clusters), np.nan, dtype=np.float32),
        "windDirSin": np.full((len(time_index), num_clusters), np.nan, dtype=np.float32),
        "windDirCos": np.full((len(time_index), num_clusters), np.nan, dtype=np.float32),
    }

    time_lookup = {timestamp: idx for idx, timestamp in enumerate(time_index)}
    for (time_val, cluster_id), values in aggregation.items():
        idx = time_lookup.get(time_val)
        if idx is None or cluster_id < 0 or cluster_id >= num_clusters:
            continue
        temp_sum, wind_sum, sin_sum, cos_sum, count = values
        if count == 0:
            continue
        feature_arrays["temperature"][idx, cluster_id] = temp_sum / count
        feature_arrays["windSpeed"][idx, cluster_id] = wind_sum / count
        feature_arrays["windDirSin"][idx, cluster_id] = sin_sum / count
        feature_arrays["windDirCos"][idx, cluster_id] = cos_sum / count

    features = []
    for feature_name, raw_array in feature_arrays.items():
        df_feature = pd.DataFrame(raw_array, index=time_index, columns=range(num_clusters))
        df_feature = df_feature.interpolate(method="time").ffill().bfill().fillna(0.0)
        features.append(df_feature.to_numpy())

    stacked = np.stack(features, axis=-1)  # (time, clusters, features)
    return stacked, time_index


def haversine_distance(lat1, lon1, lat2, lon2):
    """Great-circle distance in kilometers between two lat/lon points."""
    radius = 6371.0
    phi1, phi2 = math.radians(lat1), math.radians(lat2)
    dphi = math.radians(lat2 - lat1)
    dlambda = math.radians(lon2 - lon1)
    a = math.sin(dphi / 2) ** 2 + math.cos(phi1) * math.cos(phi2) * math.sin(dlambda / 2) ** 2
    return 2 * radius * math.asin(math.sqrt(a))


def build_adjacency(centers: np.ndarray, k_neighbors: int) -> np.ndarray:
    """Build normalized adjacency matrix based on k-nearest neighbors."""
    num_nodes = centers.shape[0]
    distances = np.zeros((num_nodes, num_nodes), dtype=np.float32)
    for i in range(num_nodes):
        for j in range(num_nodes):
            if i == j:
                continue
            distances[i, j] = haversine_distance(centers[i][0], centers[i][1], centers[j][0], centers[j][1])

    adjacency = np.zeros((num_nodes, num_nodes), dtype=np.float32)
    for i in range(num_nodes):
        neighbors = np.argsort(distances[i])[: k_neighbors + 1]
        adjacency[i, neighbors] = 1.0

    adjacency = np.maximum(adjacency, adjacency.T)
    adjacency += np.eye(num_nodes, dtype=np.float32)
    degree = np.sum(adjacency, axis=1)
    degree_inv_sqrt = np.diag(1.0 / np.sqrt(degree))
    normalized = degree_inv_sqrt @ adjacency @ degree_inv_sqrt
    return normalized


def build_datasets(features: np.ndarray, cfg: Config) -> Tuple[TensorDataset, TensorDataset, StandardScaler]:
    """Create windowed datasets for supervised training."""
    logging.info("Building time window datasets")
    num_steps, num_nodes, num_features = features.shape
    sequences = []
    targets = []
    for t in range(num_steps - cfg.input_window - 1):
        sequences.append(features[t : t + cfg.input_window])
        targets.append(features[t + cfg.input_window])

    sequences = np.stack(sequences)
    targets = np.stack(targets)

    split_idx = int(len(sequences) * cfg.train_split)
    train_x, val_x = sequences[:split_idx], sequences[split_idx:]
    train_y, val_y = targets[:split_idx], targets[split_idx:]

    scaler = StandardScaler()
    scaler.fit(train_x.reshape(-1, num_features))

    train_x = scaler.transform(train_x.reshape(-1, num_features)).reshape(train_x.shape)
    val_x = scaler.transform(val_x.reshape(-1, num_features)).reshape(val_x.shape)
    train_y = scaler.transform(train_y.reshape(-1, num_features)).reshape(train_y.shape)
    val_y = scaler.transform(val_y.reshape(-1, num_features)).reshape(val_y.shape)

    train_ds = TensorDataset(
        torch.tensor(train_x, dtype=torch.float32),
        torch.tensor(train_y, dtype=torch.float32),
    )
    val_ds = TensorDataset(
        torch.tensor(val_x, dtype=torch.float32),
        torch.tensor(val_y, dtype=torch.float32),
    )
    return train_ds, val_ds, scaler


def train_model(cfg: Config, train_ds: TensorDataset, val_ds: TensorDataset, adjacency: torch.Tensor) -> GraphTemporalModel:
    """Train the graph-temporal model using mini-batch optimization."""
    model = GraphTemporalModel(
        in_features=train_ds.tensors[0].shape[-1],
        hidden_features=64,
        out_features=train_ds.tensors[1].shape[-1],
        adjacency=adjacency,
    ).to(cfg.device)

    optimizer = optim.Adam(model.parameters(), lr=cfg.learning_rate)
    loss_fn = nn.MSELoss()

    train_loader = DataLoader(train_ds, batch_size=cfg.batch_size, shuffle=True)
    val_loader = DataLoader(val_ds, batch_size=cfg.batch_size, shuffle=False)

    logging.info("Starting training with %d epochs", cfg.epochs)

    for epoch in range(cfg.epochs):
        model.train()
        train_loss = 0.0
        for batch_x, batch_y in train_loader:
            batch_x = batch_x.to(cfg.device)
            batch_y = batch_y.to(cfg.device)
            optimizer.zero_grad()
            pred = model(batch_x)
            loss = loss_fn(pred, batch_y)
            loss.backward()
            optimizer.step()
            train_loss += loss.item() * batch_x.size(0)

        model.eval()
        val_loss = 0.0
        with torch.no_grad():
            for batch_x, batch_y in val_loader:
                batch_x = batch_x.to(cfg.device)
                batch_y = batch_y.to(cfg.device)
                pred = model(batch_x)
                loss = loss_fn(pred, batch_y)
                val_loss += loss.item() * batch_x.size(0)

        logging.info(
            "Epoch %d/%d (%.2f%%) | Train Loss: %.6f | Val Loss: %.6f",
            epoch + 1,
            cfg.epochs,
            (epoch + 1) / cfg.epochs * 100,
            train_loss / len(train_ds),
            val_loss / len(val_ds),
        )

    return model


def save_artifacts(cfg: Config, model: GraphTemporalModel, scaler: StandardScaler, kmeans: MiniBatchKMeans, adjacency: np.ndarray):
    """Persist model weights and metadata to disk."""
    cfg.artifacts_dir.mkdir(parents=True, exist_ok=True)
    model_path = cfg.artifacts_dir / "model.pt"
    scaler_path = cfg.artifacts_dir / "scaler.json"
    metadata_path = cfg.artifacts_dir / "metadata.json"

    torch.save(
        {
            "model_state": model.state_dict(),
            "input_window": cfg.input_window,
            "num_clusters": cfg.num_clusters,
        },
        model_path,
    )

    scaler_payload = {
        "mean": scaler.mean_.tolist(),
        "scale": scaler.scale_.tolist(),
    }
    scaler_path.write_text(json.dumps(scaler_payload, indent=2))

    metadata_payload = {
        "cluster_centers": kmeans.cluster_centers_.tolist(),
        "adjacency": adjacency.tolist(),
        "feature_order": ["temperature", "windSpeed", "windDirSin", "windDirCos"],
    }
    metadata_path.write_text(json.dumps(metadata_payload, indent=2))

    logging.info("Artifacts saved to %s", cfg.artifacts_dir)


def main():
    """Entry point for model training."""
    logging.info("Starting ModelAI training pipeline...")
    cfg = Config()
    logging.info("Training on device: %s", cfg.device)
    kmeans = fit_clusters(cfg)
    features, _ = stream_aggregate_clusters(cfg, kmeans)
    adjacency = build_adjacency(kmeans.cluster_centers_, cfg.k_neighbors)

    train_ds, val_ds, scaler = build_datasets(features, cfg)
    adjacency_tensor = torch.tensor(adjacency, dtype=torch.float32, device=cfg.device)

    model = train_model(cfg, train_ds, val_ds, adjacency_tensor)
    save_artifacts(cfg, model, scaler, kmeans, adjacency)
    logging.info("ModelAI training pipeline completed successfully.")


if __name__ == "__main__":
    main()
