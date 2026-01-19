import os
import streamlit as st
import pandas as pd
from mongo_manager import MongoManager
import json

def main():
    st.set_page_config(page_title="Mongo Explorer", layout="wide")
    st.title("MongoDB Explorer")

    # Connection
    mongo_uri = os.getenv("MONGO_URI", "mongodb://localhost:27017")
    db_name = os.getenv("MONGO_DB", "WeatherDb")
    
    if 'manager' not in st.session_state:
        st.session_state.manager = MongoManager(mongo_uri)

    manager = st.session_state.manager

    # Sidebar - Navigation
    st.sidebar.header("Connection")
    st.sidebar.text(f"URI: {mongo_uri}")
    
    try:
        dbs = manager.list_databases()
        selected_db = st.sidebar.selectbox("Select Database", dbs, index=dbs.index(db_name) if db_name in dbs else 0)
        
        collections = manager.list_collections(selected_db)
        selected_col = st.sidebar.selectbox("Select Collection", collections)
        
        st.sidebar.markdown("---")
        action = st.sidebar.radio("Action", ["View Data", "Create Item", "Update Item", "Delete Item"])
        
        if selected_db and selected_col:
            st.header(f"{selected_db}.{selected_col}")
            
            if action == "View Data":
                view_data(manager, selected_db, selected_col)
            elif action == "Create Item":
                create_item(manager, selected_db, selected_col)
            elif action == "Update Item":
                update_item(manager, selected_db, selected_col)
            elif action == "Delete Item":
                delete_item(manager, selected_db, selected_col)
                
    except Exception as e:
        st.error(f"Connection Error: {e}")

def view_data(manager, db, col):
    page_size = 10
    if 'page' not in st.session_state:
        st.session_state.page = 0
        
    total_docs = manager.count_documents(db, col)
    st.write(f"Total Documents: {total_docs}")
    
    col1, col2, col3 = st.columns([1, 2, 1])
    with col1:
        if st.button("Previous"):
            if st.session_state.page > 0:
                st.session_state.page -= 1
    with col3:
        if st.button("Next"):
            st.session_state.page += 1

    skip = st.session_state.page * page_size
    data = manager.get_data(db, col, skip=skip, limit=page_size)
    
    if data:
        # Convert ObjectId to string for display
        for d in data:
            d['_id'] = str(d['_id'])
        
        df = pd.DataFrame(data)
        st.dataframe(df)
        st.text(f"Showing {skip} - {skip + len(data)}")
    else:
        st.info("No data found")

def create_item(manager, db, col):
    st.subheader("Create New Document")
    json_input = st.text_area("JSON Document", "{\n  \"field\": \"value\"\n}", height=200)
    if st.button("Insert"):
        try:
            doc = json.loads(json_input)
            result = manager.insert_document(db, col, doc)
            st.success(f"Inserted document with ID: {result.inserted_id}")
        except Exception as e:
            st.error(f"Error: {e}")

def update_item(manager, db, col):
    st.subheader("Update Document")
    doc_id = st.text_input("Document ID (Hex String)")
    json_input = st.text_area("Update Fields (JSON)", "{\n  \"field\": \"new_value\"\n}", height=200)
    
    if st.button("Update"):
        try:
            update_dict = json.loads(json_input)
            modified = manager.update_document(db, col, doc_id, update_dict)
            st.success(f"Modified {modified} document(s)")
        except Exception as e:
            st.error(f"Error: {e}")

def delete_item(manager, db, col):
    st.subheader("Delete Document")
    delete_mode = st.radio("Mode", ["Single ID", "Filter (JSON)"])
    
    if delete_mode == "Single ID":
        doc_id = st.text_input("Document ID to delete")
        if st.button("Delete"):
            try:
                deleted = manager.delete_document(db, col, doc_id)
                st.success(f"Deleted {deleted} document(s)")
            except Exception as e:
                st.error(f"Error: {e}")
    else:
        filter_json = st.text_area("Filter JSON", "{}", height=100)
        if st.button("Delete Matching"):
            try:
                filter_dict = json.loads(filter_json)
                deleted = manager.delete_documents(db, col, filter_dict)
                st.success(f"Deleted {deleted} document(s)")
            except Exception as e:
                st.error(f"Error: {e}")

if __name__ == "__main__":
    main()
