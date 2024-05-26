# MicrosoftPhiClient
 Connect to a Flask server and submit LLM Requests

# Dependencies
 You may need to install the cuda framework from <br />
 https://developer.nvidia.com/cuda-downloads?target_os=Windows <br />
# Python environment
Create a virtual environment (optional but recommended): <br />
<br />
This keeps your installations and project separate from your main Python installation. <br />
<br />
<br />
On a command line go to where you want to create the python environment <br />
python -m venv myenv <br />
From the same command line run  <br />
myenv\Scripts\activate.bat <br />
<br />
pip install transformers accelerate bitsandbytes <br />
pip install Flask <br />
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu121 <br />

navigate to the phi-py folder on the command line <br />
python serve.py <br />
