# Celestial Experiments

This directory contains a script to automate running Celestial experiments. The script modifies satellite configurations, generates network setups, and executes Celestial server and client processes.

## What This Script Does

1. **Checks for Root Permissions**: The script must be executed with `sudo`.
2. **Defines Satellite Configurations**: It iterates over different satellite setups (planes × satellites per plane).
3. **Modifies `quickstart.toml`**: Updates the number of planes and satellites dynamically.
4. **Generates Satellite Configuration**: Runs `satgen.py` to create a network configuration.
5. **Starts Celestial Server**: Launches `celestial.bin` with a defined network interface.
6. **Runs the Client**: Executes `celestial.py` with the generated network file.
7. **Logs Execution Time**: Measures and logs the time taken for each run.

## Prerequisites

Ensure you have the following installed:

- **Python 3.12+**
- **Python `venv` and `setuptools`**
- **Celestial installed** (Follow [Celestial Documentation](https://openfogstack.github.io/celestial/quickstart.html))

## Setup

1. Install required dependencies:

```sh
sudo apt update && sudo apt install -y ipset
```

2. Create and activate a Python virtual environment:

```sh
python3 -m venv ~/celestial/.venv
source ~/celestial/.venv/bin/activate
```

## Running the Experiment

1. Execute the script with `sudo`:

```sh
sudo ./run_experiment.sh
```

## Configurable Parameters

You may need to adjust the following variables inside the script:

- **Satellite Configuration** (`SATELLITE_CONFIGS`) → Modify the number of planes and satellites per plane.
- **File Paths**
  - `CELESTIAL_DIR`: Path to Celestial's `quick-start` directory.
  - `SATGEN_SCRIPT`: Path to `satgen.py`.
  - `CELESTIAL_BIN`: Path to `celestial.bin`.
  - `CELESTIAL_SCRIPT`: Path to `celestial.py`.
  - `VENV_DIR`: Path to the Python virtual environment.
- **Network Interface** (`NETWORK_INTERFACE`) → Adjust this if using a different network adapter.
- **Server IP** (`SERVER_IP`) → Modify to match your setup.


