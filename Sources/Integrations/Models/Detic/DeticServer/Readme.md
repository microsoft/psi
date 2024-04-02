# Installation

Adapted instructions from: https://github.com/facebookresearch/Detic/blob/main/docs/INSTALL.md

In order to run the Detic server, a Python environment must first be created manually following these instructions.

### Example Conda environment setup

Create a new environment, clone the Detic repo and install dependencies:
```
conda create --name detic python==3.9 -y
conda activate detic
pip install torch torchvision torchaudio --extra-index-url https://download.pytorch.org/whl/cu118

# Under your working directory
git clone https://github.com/facebookresearch/detectron2.git
cd detectron2
pip install -e .

cd ..
git clone https://github.com/facebookresearch/Detic.git --recurse-submodules
cd Detic
pip install -r requirements.txt

# Install additional dependencies required by the Detic server
pip install zmq msgpack
```

Download the model file from https://dl.fbaipublicfiles.com/detic/Detic_LCOCOI21k_CLIP_SwinB_896b32_4x_ft4x_max-size.pth and place it into a sub-directory named `models` under the `Detic` directory, e.g.:
```

# Create the Detic models directory
mkdir models

# Download the model file (or manually via a browser)
wget https://dl.fbaipublicfiles.com/detic/Detic_LCOCOI21k_CLIP_SwinB_896b32_4x_ft4x_max-size.pth -O models/Detic_LCOCOI21k_CLIP_SwinB_896b32_4x_ft4x_max-size.pth
```

Add the `Detic` and `Detic/third_party/CenterNet2` directories created above to the `PYTHONPATH` environment variable:

```
# On Windows
set PYTHONPATH=/path/to/Detic;/path/to/Detic/third_party/CenterNet2

# On Linux
export PYTHONPATH=/path/to/Detic:/path/to/Detic/third_party/CenterNet2
```

To automatically set the `PYTHONPATH` environment variable when the Conda environment is activated:
```
# On Windows
conda env config vars set PYTHONPATH=/path/to/Detic;/path/to/Detic/third_party/CenterNet2

# On Linux
conda env config vars set PYTHONPATH=/path/to/Detic:/path/to/Detic/third_party/CenterNet2
```

Finally, to run the Detic server:
```
python /path/to/psi/Sources/Integrations/Models/Detic/DeticServer/detic_server.py
```
