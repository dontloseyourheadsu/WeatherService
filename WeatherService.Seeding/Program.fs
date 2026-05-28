open System
open MongoDB.Driver
open MongoDB.Bson

[<EntryPoint>]
let main argv =
    printfn "Starting Weather Service F# Database Seeder..."
    
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
    
    printfn "Connecting to MongoDB..."
    
    try
        let client = new MongoClient(connectionUri)
        let database = client.GetDatabase(dbName)
        let collection = database.GetCollection<BsonDocument>(collectionName)
        
        // Wait for MongoDB container to be ready
        let mutable connected = false
        let mutable attempts = 0
        let maxAttempts = 15
        
        while not connected && attempts < maxAttempts do
            attempts <- attempts + 1
            try
                printfn "Connection attempt %d of %d..." attempts maxAttempts
                // Ping the database to check if connection is active
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
        
        // Ensure the unique index exists (timestamp, latitude, longitude)
        printfn "Ensuring unique index exists..."
        let keys = Builders<BsonDocument>.IndexKeys.Ascending("timestamp").Ascending("latitude").Ascending("longitude")
        let indexOptions = CreateIndexOptions(Unique = true, Name = "timestamp_lat_lon_unique")
        let indexModel = CreateIndexModel<BsonDocument>(keys, indexOptions)
        collection.Indexes.CreateOne(indexModel) |> ignore
        
        // Check if database is empty
        let count = collection.CountDocuments(Builders<BsonDocument>.Filter.Empty)
        printfn "Current document count in Forecasts collection: %d" count
        
        if count = 0L then
            printfn "Database is empty! Seeding initial weather forecasts..."
            
            // Seed data for the current hour and surrounding hours
            let nowUtc = DateTime.UtcNow
            let currentHour = DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, 0, 0, DateTimeKind.Utc)
            
            // We will seed data for several cities
            let locations = [
                ("Mexico City", 19.4326, -99.1332)
                ("New York", 40.7128, -74.0060)
                ("London", 51.5074, -0.1278)
                ("Tokyo", 35.6762, 139.6503)
                ("Sydney", -33.8688, 151.2093)
            ]
            
            let random = Random()
            let documents = ResizeArray<BsonDocument>()
            
            // For each location, seed forecast data for past 12 hours and next 12 hours
            for (cityName, lat, lon) in locations do
                printfn "Generating forecasts for %s (%f, %f)..." cityName lat lon
                for hourOffset in -12 .. 12 do
                    let timestamp = currentHour.AddHours(double hourOffset)
                    let temp = 15.0 + 10.0 * Math.Sin(double hourOffset / 4.0) + random.NextDouble() * 3.0
                    let windSpeed = 5.0 + random.NextDouble() * 15.0
                    let windDir = random.Next(0, 360)
                    let sunrise = DateTime(timestamp.Year, timestamp.Month, timestamp.Day, 6, 0, 0, DateTimeKind.Utc)
                    
                    let doc = BsonDocument()
                    doc.Add("timestamp", BsonDateTime(timestamp)) |> ignore
                    doc.Add("latitude", BsonDouble(lat)) |> ignore
                    doc.Add("longitude", BsonDouble(lon)) |> ignore
                    doc.Add("temperature", BsonDouble(Math.Round(temp, 1))) |> ignore
                    doc.Add("temperatureUnit", BsonString("°C")) |> ignore
                    doc.Add("windSpeed", BsonDouble(Math.Round(windSpeed, 1))) |> ignore
                    doc.Add("windSpeedUnit", BsonString("km/h")) |> ignore
                    doc.Add("windDirection", BsonInt32(windDir)) |> ignore
                    doc.Add("windDirectionUnit", BsonString("°")) |> ignore
                    doc.Add("sunrise", BsonDateTime(sunrise)) |> ignore
                    
                    documents.Add(doc)
            
            printfn "Inserting %d documents..." documents.Count
            collection.InsertMany(documents)
            printfn "Seeding completed successfully!"
        else
            printfn "Database already has data. Skipping seeding."
        0
    with
    | ex ->
        printfn "An error occurred: %s" ex.Message
        printfn "%s" ex.StackTrace
        1
