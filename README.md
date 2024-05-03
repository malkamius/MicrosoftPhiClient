# MicrosoftPhiClient
 Connect to a Flask server and submit LLM Requests

# Dependencies
 You may need to install the cude framework from 
 https://developer.nvidia.com/cuda-downloads?target_os=Windows
# Python environment
Create a virtual environment (optional but recommended):

This keeps your installations and project separate from your main Python installation.
bash

On a command line go to where you want to create the python environment
python -m venv myenv
From the same command line run 
myenv\Scripts\activate.bat

pip install accelerate
pip install Flask
pip install transformers
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu121

navigate to the phi-py folder on the command line
python serve.py
