import os
import json
from mongo_manager import MongoManager

def run_cli():
    mongo_uri = os.getenv("MONGO_URI", "mongodb://localhost:27017")
    manager = MongoManager(mongo_uri)
    
    print(f"Connected to MongoDB at {mongo_uri}")
    
    while True:
        print("\n--- MongoDB Explorer CLI ---")
        print("1. List Databases")
        print("2. List Collections")
        print("3. View Data")
        print("4. Insert Document")
        print("5. Update Document")
        print("6. Delete Document")
        print("0. Exit")
        
        choice = input("Select option: ")
        
        if choice == "0":
            break
        elif choice == "1":
            print("Databases:", manager.list_databases())
        elif choice == "2":
            db_name = input("Enter database name: ")
            try:
                print("Collections:", manager.list_collections(db_name))
            except Exception as e:
                print(f"Error: {e}")
        elif choice == "3":
            db = input("Database: ")
            col = input("Collection: ")
            page_size = 10
            page = 0
            
            while True:
                data = manager.get_data(db, col, skip=page*page_size, limit=page_size)
                if not data:
                    print("No more data.")
                    break
                    
                print(f"\n--- Page {page} ---")
                for doc in data:
                    # Convert ObjectId to str for printing
                    doc['_id'] = str(doc['_id'])
                    print(json.dumps(doc, indent=2))
                
                nav = input("\n(n)ext, (p)rev, (q)uit view: ").lower()
                if nav == 'n':
                    page += 1
                elif nav == 'p' and page > 0:
                    page -= 1
                elif nav == 'q':
                    break
                    
        elif choice == "4":
            db = input("Database: ")
            col = input("Collection: ")
            print("Enter JSON document (press Enter, then Ctrl+D or Ctrl+Z to finish):")
            # Simple single line input for now to avoid complexity in basic cli
            json_str = input("JSON: ")
            try:
                doc = json.loads(json_str)
                res = manager.insert_document(db, col, doc)
                print(f"Inserted ID: {res.inserted_id}")
            except Exception as e:
                print(f"Error: {e}")

        elif choice == "5":
            db = input("Database: ")
            col = input("Collection: ")
            ids = input("Document ID to update: ")
            json_str = input("Enter fields to update (JSON): ")
            try:
                update_doc = json.loads(json_str)
                modified = manager.update_document(db, col, ids, update_doc)
                print(f"Modified {modified} documents")
            except Exception as e:
                print(f"Error: {e}")
                
        elif choice == "6":
            db = input("Database: ")
            col = input("Collection: ")
            mode = input("Delete by (i)d or (f)ilter? ").lower()
            if mode == 'i':
                ids = input("Document ID: ")
                deleted = manager.delete_document(db, col, ids)
            else:
                json_str = input("Filter JSON: ")
                try:
                    filter_doc = json.loads(json_str)
                    deleted = manager.delete_documents(db, col, filter_doc)
                except Exception as e:
                    print(f"Error: {e}")
                    continue
            print(f"Deleted {deleted} documents")
