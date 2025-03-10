# StarryNet Experiments

This folder contains the StarryNet experiments, which serve as one of the baselines for Stardust.

## Setup

Please ensure that you have the following prerequisites installed:
- Docker
- Python 3.12 or higher
- Python 3 `venv` and `setuptools` packages

To install the StarryNet dependencies, open a terminal in the [Experiments](../) folder and execute the following commands.

```sh
# Create a virtual environment
python3 -m venv StarryNet

# Activate the virtual environment
cd StarryNet
source ./bin/activate # For bash (see bin folder for other shells)

# Install dependencies
pip install -r ./StarryNet/tools/requirements.txt
```

For StarryNet to work, Docker must be executed in swarm mode.

```sh
docker swarm init
```

StarryNet will always attempt to connect to a remote machine via SSH to run the containers that represent the satellites.
You can run it locally by creating an additional user that has the permission to start Docker containers (i.e., the user must be part of the `docker` group).
Enter the credentials of this user in the [StarryNet/config.json](./StarryNet/config.json) file.


## Run Experiments

To run the experiments open a terminal in this folder and execute the following commands.

```sh
## Activate the virtual environment created during setup
source ./bin/activate # For bash (see bin folder for other shells)

cd StarryNet
python example.py
```