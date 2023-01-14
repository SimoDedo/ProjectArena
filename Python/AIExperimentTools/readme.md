## Contents of this folder

This folder contains some Python scripts to run different AI-based tests, such as map genetic evolution or weapon balance analysis.
The scripts contained in this folder are configured using command line parameter. For any doubt, running the scripts with `--help` should allow one to check how to use them.

Using this scripts requires a Unity build of the project containing the following Scenes:
- `Scenes/AITesting/Start` (first scene in the build);
- `Scenes/AITesting/GenomeTester`

The location of this build and of the `Data` folder used to interact with the Unity build must be specified by editing `internals/constants.py`.
A sample `Data` folder is included here.

## Possible issues
Due to some unknown Unity or OS (Linux) bug, running multiple instances of Unity might deadlock if the audio system is not explicitly disabled in the build.
