open System
open System.Net.Http
open System.Text.Json
open MongoDB.Driver
open MongoDB.Bson

// Fetch historical data from Open-Meteo Archive API
let fetchHistoricalData (client: HttpClient) (lat: double) (lon: double) (startDate: string) (endDate: string) =
    async {
        let url = sprintf "https://archive-api.open-meteo.com/v1/archive?latitude=%f&longitude=%f&start_date=%s&end_date=%s&hourly=temperature_2m,wind_speed_10m,wind_direction_10m" lat lon startDate endDate
        printfn "Fetching data from Open-Meteo: %s" url
        try
            let! response = client.GetStringAsync(url) |> Async.AwaitTask
            return Some response
        with
        | ex ->
            printfn "Error fetching data for (%f, %f): %s" lat lon ex.Message
            return None
    }

// Parse Open-Meteo response to BsonDocuments
let parseJsonToBsonDocuments (jsonStr: string) (zoneName: string) =
    try
        use doc = JsonDocument.Parse(jsonStr)
        let root = doc.RootElement
        let latitude = root.GetProperty("latitude").GetDouble()
        let longitude = root.GetProperty("longitude").GetDouble()
        let hourly = root.GetProperty("hourly")
        let times = hourly.GetProperty("time")
        let temps = hourly.GetProperty("temperature_2m")
        let windSpeeds = hourly.GetProperty("wind_speed_10m")
        let windDirs = hourly.GetProperty("wind_direction_10m")
        
        let count = times.GetArrayLength()
        let documents = ResizeArray<BsonDocument>()
        
        for i in 0 .. count - 1 do
            if not (temps.[i].ValueKind = JsonValueKind.Null) && 
               not (windSpeeds.[i].ValueKind = JsonValueKind.Null) && 
               not (windDirs.[i].ValueKind = JsonValueKind.Null) then
                
                let timeStr = times.[i].GetString()
                let timestamp = DateTime.Parse(timeStr, null, System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime()
                let temp = temps.[i].GetDouble()
                let speed = windSpeeds.[i].GetDouble()
                let dir = windDirs.[i].GetInt32()
                let sunrise = DateTime(timestamp.Year, timestamp.Month, timestamp.Day, 6, 0, 0, DateTimeKind.Utc)
                
                let doc = BsonDocument()
                doc.Add("timestamp", BsonDateTime(timestamp)) |> ignore
                doc.Add("latitude", BsonDouble(latitude)) |> ignore
                doc.Add("longitude", BsonDouble(longitude)) |> ignore
                doc.Add("temperature", BsonDouble(Math.Round(temp, 1))) |> ignore
                doc.Add("temperatureUnit", BsonString("°C")) |> ignore
                doc.Add("windSpeed", BsonDouble(Math.Round(speed, 1))) |> ignore
                doc.Add("windSpeedUnit", BsonString("km/h")) |> ignore
                doc.Add("windDirection", BsonInt32(dir)) |> ignore
                doc.Add("windDirectionUnit", BsonString("°")) |> ignore
                doc.Add("sunrise", BsonDateTime(sunrise)) |> ignore
                doc.Add("zone", BsonString(zoneName)) |> ignore
                
                documents.Add(doc)
        documents
    with
    | ex ->
        printfn "Error parsing JSON response: %s" ex.Message
        ResizeArray<BsonDocument>()

[<EntryPoint>]
let main argv =
    printfn "Starting Weather Service F# Database Seeder (Open-Meteo Historical Archive)..."
    
    // Get MongoDB connection Uri
    let connectionUri = 
        match Environment.GetEnvironmentVariable("ConnectionStrings__WeatherDb") with
        | null | "" -> 
            match Environment.GetEnvironmentVariable("MONGO_URI") with
            | null | "" -> "mongodb://localhost:27017"
            | uri -> uri
        | uri -> uri
        
    let dbName = 
        match Environment.GetEnvironmentVariable("MONGO_DB") with
        | null | "" -> "WeatherDb"
        | db -> db
        
    let collectionName = "Forecasts"
    
    printfn "Connecting to MongoDB at %s..." connectionUri
    
    try
        let client = new MongoClient(connectionUri)
        let database = client.GetDatabase(dbName)
        
        // Wait for MongoDB container to be ready
        let mutable connected = false
        let mutable attempts = 0
        let maxAttempts = 15
        
        while not connected && attempts < maxAttempts do
            attempts <- attempts + 1
            try
                printfn "Connection attempt %d of %d..." attempts maxAttempts
                database.RunCommand(JsonCommand<BsonDocument>("{ping: 1}")) |> ignore
                connected <- true
                printfn "Successfully connected to MongoDB!"
            with
            | ex ->
                if attempts >= maxAttempts then
                    reraise()
                else
                    printfn "Connection failed: %s. Waiting 2 seconds before retrying..." ex.Message
                    System.Threading.Thread.Sleep(2000)

        // Ensure collection is capped at 5GB (5368709120 bytes)
        let collectionsList = database.ListCollections().ToList()
        let existingColl = collectionsList |> Seq.tryFind (fun doc -> doc.["name"].AsString = collectionName)
        let mutable isCapped = false
        
        match existingColl with
        | Some doc ->
            if doc.Contains("options") && doc.["options"].IsBsonDocument then
                let opts = doc.["options"].AsBsonDocument
                if opts.Contains("capped") && opts.["capped"].IsBoolean then
                    isCapped <- opts.["capped"].AsBoolean
            
            if not isCapped then
                printfn "Collection 'Forecasts' exists but is not capped. Re-creating as capped (5GB)..."
                database.DropCollection(collectionName)
                let options = CreateCollectionOptions(Capped = true, MaxSize = 5368709120L)
                database.CreateCollection(collectionName, options)
            else
                printfn "Collection 'Forecasts' is already capped."
        | None ->
            printfn "Creating capped collection 'Forecasts' of 5GB..."
            let options = CreateCollectionOptions(Capped = true, MaxSize = 5368709120L)
            database.CreateCollection(collectionName, options)

        let collection = database.GetCollection<BsonDocument>(collectionName)

        // Ensure the unique index exists (timestamp, latitude, longitude)
        printfn "Ensuring unique index exists..."
        let keys = Builders<BsonDocument>.IndexKeys.Ascending("timestamp").Ascending("latitude").Ascending("longitude")
        let indexOptions = CreateIndexOptions(Unique = true, Name = "timestamp_lat_lon_unique")
        let indexModel = CreateIndexModel<BsonDocument>(keys, indexOptions)
        collection.Indexes.CreateOne(indexModel) |> ignore
        
        // Check current document count
        let count = collection.CountDocuments(Builders<BsonDocument>.Filter.Empty)
        printfn "Current document count in Forecasts collection: %d" count
        
        if count = 0L then
            printfn "Database is empty! Seeding historical weather forecasts using Open-Meteo..."
            
            // Define 5 zones with 3 coordinates each (Center, North, South)
            let zones = [
                ("Mexico City", [
                    (19.4326, -99.1332)
                    (19.5326, -99.1332)
                    (19.3326, -99.1332)
                ])
                ("New York", [
                    (40.7128, -74.0060)
                    (40.8128, -74.0060)
                    (40.6128, -74.0060)
                ])
                ("London", [
                    (51.5074, -0.1278)
                    (51.6074, -0.1278)
                    (51.4074, -0.1278)
                ])
                ("Tokyo", [
                    (35.6762, 139.6503)
                    (35.7762, 139.6503)
                    (35.5762, 139.6503)
                ])
                ("Sydney", [
                    (-33.8688, 151.2093)
                    (-33.7688, 151.2093)
                    (-33.9688, 151.2093)
                ])
            ]
            
            // Dates to query: past 12 months relative to 2026-06-01
            let startDate = "2025-06-01"
            let endDate = "2026-06-01"
            
            use httpClient = new HttpClient()
            
            for (zoneName, coords) in zones do
                printfn "Processing Zone: %s" zoneName
                for (lat, lon) in coords do
                    printfn "Requesting historical data for (%f, %f)..." lat lon
                    let responseOpt = fetchHistoricalData httpClient lat lon startDate endDate |> Async.RunSynchronously
                    match responseOpt with
                    | Some jsonStr ->
                        let docs = parseJsonToBsonDocuments jsonStr zoneName
                        if docs.Count > 0 then
                            printfn "Inserting %d documents for (%f, %f) in zone %s..." docs.Count lat lon zoneName
                            
                            // Insert in chunks of 5000 to prevent BSON document size issues or timeout
                            let chunkSize = 5000
                            let totalDocs = docs.Count
                            for k in 0 .. chunkSize .. totalDocs - 1 do
                                let chunk = docs.GetRange(k, min chunkSize (totalDocs - k))
                                collection.InsertMany(chunk)
                        else
                            printfn "No records parsed for (%f, %f)." lat lon
                    | None ->
                        printfn "Failed to retrieve data for (%f, %f)." lat lon
                    
                    // Add delay to prevent hitting API rate limits
                    System.Threading.Thread.Sleep(500)
            
            printfn "Seeding completed successfully!"
        else
            printfn "Database already has data. Skipping seeding."
        0
    with
    | ex ->
        printfn "An error occurred: %s" ex.Message
        printfn "%s" ex.StackTrace
        1
