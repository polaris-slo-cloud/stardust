#!/bin/bash
# set -x

## This script executes the StarryNet 100 simulation steps experiment with multiple number os satellites
## and logs the end-to-end execution time to an output file.

SATELLITE_COUNTS=(
    "250"
    "500"
    "1000"
    "2000"
    "3000"
    "6000"
    "7000"
    "13764"
    "20646"
)

OUTPUT_FILE="./experiment.log"

date > "$OUTPUT_FILE"
echo "All durations are measured in seconds." >> "$OUTPUT_FILE"
cd starrynet
OUTPUT_FILE="../$OUTPUT_FILE"

for satCount in "${SATELLITE_COUNTS[@]}"; do
    echo "Satellites: $satCount" | tee -a "$OUTPUT_FILE"
    export STARRYNET_SATS=$satCount

    startTime=$SECONDS
    python experiment.py
    endTime=$SECONDS
    duration=$((endTime - starTime))
    echo "Duration: $duration" | tee -a "$OUTPUT_FILE"

    # Cleanup
    echo "Cleaning up"
    startTime=$SECONDS
    docker service rm constellation-test
    docker stop $(docker ps -a -q)
    docker container prune -f
    docker network prune -f
    endTime=$SECONDS
    duration=$((endTime - starTime))
    echo "Cleanup: $duration" | tee -a "$OUTPUT_FILE"

    # Add an empty line
    echo "" >> "$OUTPUT_FILE"
done
