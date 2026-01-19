from pymongo import MongoClient
from bson.objectid import ObjectId
from bson.errors import InvalidId

class MongoManager:
    def __init__(self, uri):
        self.client = MongoClient(uri)

    def list_databases(self):
        return self.client.list_database_names()

    def list_collections(self, db_name):
        return self.client[db_name].list_collection_names()

    def count_documents(self, db_name, col_name):
        return self.client[db_name][col_name].count_documents({})

    def get_data(self, db_name, col_name, skip=0, limit=10):
        cursor = self.client[db_name][col_name].find().skip(skip).limit(limit)
        return list(cursor)

    def insert_document(self, db_name, col_name, doc):
        return self.client[db_name][col_name].insert_one(doc)

    def update_document(self, db_name, col_name, doc_id, update_fields):
        try:
            oid = ObjectId(doc_id)
            result = self.client[db_name][col_name].update_one(
                {'_id': oid},
                {'$set': update_fields}
            )
            return result.modified_count
        except InvalidId:
            raise ValueError(f"Invalid ObjectId: {doc_id}")

    def delete_document(self, db_name, col_name, doc_id):
        try:
            oid = ObjectId(doc_id)
            result = self.client[db_name][col_name].delete_one({'_id': oid})
            return result.deleted_count
        except InvalidId:
            raise ValueError(f"Invalid ObjectId: {doc_id}")

    def delete_documents(self, db_name, col_name, filter_doc):
        result = self.client[db_name][col_name].delete_many(filter_doc)
        return result.deleted_count
