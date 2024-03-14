# Installation

Modified instructions from: https://github.com/UX-Decoder/Segment-Everything-Everywhere-All-At-Once/blob/v1.0/assets/readmes/INSTALL.md

In order to run the SEEM server, a Python environment must first be created manually following these instructions.

### Example Conda environment setup

Create a new environment and clone the SEEM repo:
```
conda create --name seem python==3.9 -y
conda activate seem

# Under your working directory
git clone https://github.com/UX-Decoder/Segment-Everything-Everywhere-All-At-Once
cd Segment-Everything-Everywhere-All-At-Once
```

Next, install the dependencies based on Windows or Linux:
```
# On Windows
pip install -r /path/to/psi/Integrations/Models/SEEM/SEEMServer/requirements_windows.txt
pip install -r assets/requirements/requirements_custom.txt

# On Linux
pip install -r assets/requirements/requirements.txt
pip install -r assets/requirements/requirements_custom.txt

# Install additional dependencies required by the SEEM server
pip install zmq msgpack
```

Download the model file from https://huggingface.co/xdecoder/SEEM/resolve/main/seem_focall_v0.pt and place it in the `Segment-Everything-Everywhere-All-At-Once` directory.

Add the `Segment-Everything-Everywhere-All-At-Once` directory to the `PYTHONPATH` environment variable:

```
# On Windows
set PYTHONPATH=/path/to/Segment-Everything-Everywhere-All-At-Once

# On Linux
export PYTHONPATH=/path/to/Segment-Everything-Everywhere-All-At-Once
```

To automatically set the `PYTHONPATH` environment variable when the Conda environment is activated:
```
# On Windows
conda env config vars set PYTHONPATH=/path/to/Segment-Everything-Everywhere-All-At-Once

# On Linux
conda env config vars set PYTHONPATH=/path/to/Segment-Everything-Everywhere-All-At-Once
```

Finally, to run the SEEM server:
```
python /path/to/psi/Sources/Integrations/Models/SEEM/SEEMServer/seem_server.py
```
