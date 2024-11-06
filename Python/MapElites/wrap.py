from distributed import Client, LocalCluster
from archive_analysis import analyze_archive
from map_elites import evolve_maps
from internals import config as conf
from internals import constants

if __name__ == "__main__":
    # Setup Dask. The client connects to a "cluster" running on this machine.
    # The cluster simply manages several concurrent worker processes. If using
    # Dask across many workers, we would set up a more complicated cluster and
    # connect the client to it.
    cluster = LocalCluster(
        processes=True,  # Each worker is a process.
        n_workers=10,  # Create this many worker processes.
        threads_per_worker=1,  # Each worker process is single-threaded.
    )
    client = Client(cluster)

    bot1_data = {"file": conf.BOT1_FILE_PREFIX, "skill": conf.BOT1_SKILL}
    bot2_data = {"file": conf.BOT2_FILE_PREFIX, "skill": conf.BOT2_SKILL}

    archieve_analysis = True

    # Run the experiment.
    conf.REPRESENTATION_NAME = constants.ALL_BLACK_NAME
    conf.EMITTER_TYPE_NAME = constants.ALL_BLACK_EMITTER_NAME
    if not archieve_analysis:
        evolve_maps(
            bot1_data, 
            bot2_data, 
            representation=constants.ALL_BLACK_NAME,
            emitter_type=constants.ALL_BLACK_EMITTER_NAME,
            batch_size=conf.BATCH_SIZE, 
            iterations=conf.ITERATIONS, 
            n_emitters=conf.N_EMITTERS, 
            client=client, 
            folder_name=conf.folder_name(False),
            game_length=conf.GAME_LENGTH,
            )
    else:
        analyze_archive(
            representation=conf.REPRESENTATION_NAME,
            folder_name=conf.folder_name(False)
        )

    conf.REPRESENTATION_NAME = constants.GRID_GRAPH_NAME
    conf.EMITTER_TYPE_NAME = constants.GRID_GRAPH_EMITTER_NAME
    if not archieve_analysis:
        evolve_maps(
            bot1_data, 
            bot2_data, 
            representation=constants.GRID_GRAPH_NAME,
            emitter_type=constants.GRID_GRAPH_EMITTER_NAME,
            batch_size=conf.BATCH_SIZE, 
            iterations=conf.ITERATIONS, 
            n_emitters=conf.N_EMITTERS, 
            client=client, 
            folder_name=conf.folder_name(False),
            game_length=conf.GAME_LENGTH,
            )
    else:
        analyze_archive(
            representation=conf.REPRESENTATION_NAME,
            folder_name=conf.folder_name(False)
        )        
    
    conf.REPRESENTATION_NAME = constants.POINT_AD_NAME
    conf.EMITTER_TYPE_NAME = constants.POINT_AD_EMITTER_NAME
    if not archieve_analysis:
        evolve_maps(
            bot1_data, 
            bot2_data, 
            representation=constants.POINT_AD_NAME,
            emitter_type=constants.POINT_AD_EMITTER_NAME,
            batch_size=conf.BATCH_SIZE, 
            iterations=conf.ITERATIONS, 
            n_emitters=conf.N_EMITTERS, 
            client=client, 
            folder_name=conf.folder_name(False),
            game_length=conf.GAME_LENGTH,
            )
    else:
        analyze_archive(
            representation=conf.REPRESENTATION_NAME,
            folder_name=conf.folder_name(False)
        )
    
    conf.REPRESENTATION_NAME = constants.SMT_NAME
    conf.EMITTER_TYPE_NAME = constants.SMT_EMITTER_NAME
    if not archieve_analysis:
        evolve_maps(
            bot1_data, 
            bot2_data, 
            representation=constants.SMT_NAME,
            emitter_type=constants.SMT_EMITTER_NAME,
            batch_size=conf.BATCH_SIZE, 
            iterations=conf.ITERATIONS, 
            n_emitters=conf.N_EMITTERS, 
            client=client, 
            folder_name=conf.folder_name(False),
            game_length=conf.GAME_LENGTH,
            )
    else:
        analyze_archive(
            representation=conf.REPRESENTATION_NAME,
            folder_name=conf.folder_name(False)
        )