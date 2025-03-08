import math
import sys
import time

from starrynet.starrynet.sn_synchronizer import *


def run_experiment(sat_count: int):
    # Locations of the explicitly configured ground station nodes.
    gs_lat_long = [
        (50.002352, 5.148141), # ESA ground station in Redu, Belgium (part of Estrack core network: https://www.esa.int/Enabling_Support/Operations/ESA_Ground_Stations/Estrack_ESA_s_global_ground_station_network)
        (32.500649, -106.608803), # NASA ground station in White Sands, New Mexico, USA (part of NASA's Near Space network: https://www.nasa.gov/technology/space-comms/near-space-network-complexes/)
    ]

    # Since the simulation step length in StarryNet is fixed to 1 second, we have configured the duration to 100 seconds.
    # That way we can simulate 100 simulation steps, just like in the Stardust configuration.
    config_file = "./config.json"
    hello_interval = 1  # hello_interval(s) in OSPF. 1-200 are supported.

    # The configuration file has 72 Starlink orbital planes configured.
    # The total number of satellites is 72 * sats_per_orbit.
    sats_per_orbit = int(math.ceil(sat_count / 72.0))

    print('Start StarryNet.')
    sn = StarryNet(
        configuration_file_path=config_file,
        GS_lat_long=gs_lat_long,
        hello_interval=hello_interval,
        sats_per_orbit_override=sats_per_orbit,
    )
    sn.create_nodes()
    sn.create_links()
    sn.run_routing_deamon()
    sn.start_emulation()
    sn.stop_emulation()


if __name__ == '__main__':
    if len(sys.argv) != 2:
        print('Please specify the number of satellites to simulate as the first argument:')
        print('python experiment.py 200')
        exit(1)
    
    try:
        sat_count = int(sys.argv[1])
    except:
        print(f'{sys.argv[1]} is not an integer')
        exit(1)
    
    start = time.time()
    run_experiment(sat_count)
    stop = time.time()
    duration = stop - start
    print(f'Experiment took {duration} seconds')