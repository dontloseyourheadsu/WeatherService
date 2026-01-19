import os
import sys
import subprocess
from cli import run_cli

def main():
    mode = os.getenv("MODE", "cli").lower()
    
    if mode == "gui":
        print("Starting GUI mode (Streamlit)...")
        # Streamlit needs to be run as an external process
        subprocess.run(["streamlit", "run", "gui.py", "--server.port=8501", "--server.address=0.0.0.0"])
    else:
        print("Starting CLI mode...")
        run_cli()

if __name__ == "__main__":
    main()
