#!/bin/bash

# Exit on first error
set -e

# Ensure the script is run as sudo
if [[ "$EUID" -ne 0 ]]; then
    echo "This script must be run as root (sudo). Exiting."
    exit 1
fi

# Define satellite configurations (planes x sats_per_plane)
SATELLITE_CONFIGS=(
    "10 25"
    "20 25"
    "40 25"
    "50 40"
    "60 50"
    "70 100"
    "80 120"
    "90 150"
    "100 200"
)

OUTPUT_FILE="./experiment.log"
CELESTIAL_DIR="/home/spock/celestial/quick-start"
SATGEN_SCRIPT="/home/spock/celestial/satgen.py"
CELESTIAL_BIN="/home/spock/celestial/celestial.bin"
CELESTIAL_SCRIPT="/home/spock/celestial/celestial.py"
VENV_DIR="/home/spock/celestial/.venv"
SERVER_IP="128.131.57.192"
NETWORK_INTERFACE="ens3"

# Ensure script runs from Celestial directory
echo "Navigating to Celestial directory..."
cd "$CELESTIAL_DIR"

# Activate Python virtual environment
if [[ -d "$VENV_DIR" ]]; then
    echo "Activating Python virtual environment..."
    source "$VENV_DIR/bin/activate"
else
    echo "Warning: Virtual environment not found at $VENV_DIR. Running without venv."
fi

# Verify satgen.py exists
if [[ ! -f "$SATGEN_SCRIPT" ]]; then
    echo "Error: $SATGEN_SCRIPT not found. Exiting."
    exit 1
fi

# Clear previous log and start fresh
echo "Initializing experiment log..."
> "$OUTPUT_FILE"
echo "Celestial Experiment Log" > "$OUTPUT_FILE"
date >> "$OUTPUT_FILE"
echo "All durations are measured in seconds." >> "$OUTPUT_FILE"

echo "Starting experiments..."
for config in "${SATELLITE_CONFIGS[@]}"; do
    read -r PLANES SATS_PER_PLANE <<< "$config"
    TOTAL_SATS=$((PLANES * SATS_PER_PLANE))
    echo "\n============================="
    echo "Running experiment with $TOTAL_SATS satellites ($PLANES planes, $SATS_PER_PLANE sats per plane)"
    echo "=============================" | tee -a "$OUTPUT_FILE"

    # Modify quickstart.toml
echo "Updating quickstart.toml with new satellite configuration..."
    sed -i "s/planes = [0-9]*/planes = $PLANES/" quickstart.toml
    sed -i "s/sats = [0-9]*/sats = $SATS_PER_PLANE/" quickstart.toml

    startTime=$SECONDS
    
    # Generate new satellite configuration
echo "Generating new satellite configuration..."
    python3 "$SATGEN_SCRIPT" quickstart.toml quickstart.zip
    
    midTime=$SECONDS
    
    # Start Celestial server in the background
echo "Starting Celestial server..."
    "$CELESTIAL_BIN" -debug --network-interface="$NETWORK_INTERFACE" 2>&1 &
    SERVER_PID=$!
    
    echo "Waiting for Celestial server to be ready..."
    sleep 3
    echo "Celestial server is ready."
    
    # Run client
echo "Running Celestial client..."
    python3 "$CELESTIAL_SCRIPT" quickstart.zip "$SERVER_IP"
    
    endTime=$SECONDS
    
    # Calculate durations
    genDuration=$((midTime - startTime))
    e2eDuration=$((endTime - startTime))
    
    echo "Configuration generation time: $genDuration seconds" | tee -a "$OUTPUT_FILE"
    echo "End-to-end execution time: $e2eDuration seconds" | tee -a "$OUTPUT_FILE"
    
done

echo "Experiment completed." | tee -a "$OUTPUT_FILE"
