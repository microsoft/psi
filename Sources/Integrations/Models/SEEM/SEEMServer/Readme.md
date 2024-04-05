# Installation

Modified instructions from: [https://github.com/UX-Decoder/Segment-Everything-Everywhere-All-At-Once/blob/v1.0/assets/readmes/INSTALL.md](https://github.com/UX-Decoder/Segment-Everything-Everywhere-All-At-Once/blob/7b2e76dbb17d0b7831c6813a921fe2bc8de22926/assets/readmes/INSTALL.md)

In order to run the SEEM server, a Python environment must first be created manually following these instructions.

### Example Conda environment setup

Create a new environment and clone the SEEM repo:
```
conda create --name seem python==3.9 -y
conda activate seem

# Under your working directory
git clone https://github.com/UX-Decoder/Segment-Everything-Everywhere-All-At-Once
cd Segment-Everything-Everywhere-All-At-Once

# Specific commit that these instructions were tested with
git checkout 7b2e76dbb17d0b7831c6813a921fe2bc8de22926
```

_**Only if running on Windows**_, edit the file 'assets/requirements/requirements.txt' as follows:
```
Replace: torch==2.1.0         with: --extra-index-url=https://download.pytorch.org/whl/cu118 torch==2.1.0+cu118
Replace: torchvision==0.16.0  with: torchvision==0.16.0+cu118
Replace: pillow==9.4.0        with: pillow==9.5.0
Delete:  deepspeed==0.10.3
```

Next, install the dependencies:
```
pip install -r assets/requirements/requirements.txt
pip install -r assets/requirements/requirements_custom.txt

# Additional dependencies required by the SEEM server
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
