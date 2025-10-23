const { MongoClient } = require("mongodb");

const uri = "mongodb://localhost:27017";
const client = new MongoClient(uri);

const databaseName = "WeatherDb";
const collections = ["Forecasts"];

async function seedDatabase() {
  try {
    await client.connect();
    console.log("Connected to MongoDB");

    const db = client.db(databaseName);

    for (const collection of collections) {
      const exists = await db.listCollections({ name: collection }).hasNext();
      if (!exists) {
        await db.createCollection(collection);
        console.log(`Created collection: ${collection}`);
      } else {
        console.log(`Collection already exists: ${collection}`);
      }
    }

    console.log("Database seeding completed.");
  } catch (error) {
    console.error("Error seeding database:", error);
  } finally {
    await client.close();
  }
}

seedDatabase();
