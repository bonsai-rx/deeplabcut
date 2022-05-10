# Bonsai - DeepLabCut
![logo](Resources/bonsai-dlc.png)

Bonsai.DeepLabCut is a [Bonsai](https://bonsai-rx.org/) interface for [DeepLabCut](https://deeplabcut.github.io/DeepLabCut) allowing real-time markerless pose estimation using pretrained models stored in the [Protocol Buffers (pb)](https://developers.google.com/protocol-buffers/) format. Natively, DLC stores the result of training as checkpoints, but it is possible to export them to .pb files using the `deeplabcut.export_model` function within DLC ([Read more here](https://github.com/AlexEMG/DeepLabCut/blob/master/docs/HelperFunctions.md#new-model-export-function)).

Bonsai.DeepLabCut loads these .pb files using [TensorFlowSharp](https://github.com/migueldeicaza/TensorFlowSharp), a set of .NET bindings for TensorFlow allowing native inference using either the CPU or GPU. By using the .pb file and the pose configuration YAML (`pose_cfg.yaml`), the `DetectPose` operator from Bonsai.DeepLabCut automatically sets up an inference graph and feeds it with live image data coming from any other Bonsai image source. The output is a `Pose` class which you can access to extract specific body parts, filter out invalid positions using a confidence threshold, or record using `CsvWriter`.

The Bonsai.DeepLabCut project was kickstarted at the [DeepLabCut Hackathon](https://github.com/DeepLabCut/DeepLabCut-Workshop-Materials) sponsored by the [Chan Zuckerberg Initiative](https://chanzuckerberg.com/) and held at Harvard University in March 2020. It has since been published in eLife ([Kane et al, eLife 2020](https://elifesciences.org/articles/61909)).

## How to install

Bonsai.DeepLabCut can be downloaded through the Bonsai package manager. In order to get visualizer support, you should download both the `Bonsai.DeepLabCut` and `Bonsai.DeepLabCut.Design` packages. However, in order to use it for either CPU or GPU inference, you need to pair it with a compiled native TensorFlow binary. You can find precompiled binaries for Windows 64-bit at https://www.tensorflow.org/install/lang_c.

To use GPU TensorFlow (highly recommended for live inference), you also need to install the CUDA Toolkit 11.2 from the [CUDA Toolkit Archive](https://developer.nvidia.com/cuda-11.2.0-download-archive), and download [cuDNN 8.1.0](https://developer.nvidia.com/cudnn) for CUDA 11.2. Make sure you have a CUDA 11 [compatible GPU](https://docs.nvidia.com/deploy/cuda-compatibility/index.html#support-hardware) with the latest NVIDIA drivers.

After downloading the native TensorFlow binary and cuDNN, you can follow these steps to get the required native files into the `Extensions` folder of your local Bonsai install:

1. The easiest way to find your Bonsai install folder is to right-click on the Bonsai shortcut > Properties. The path to the folder will be shown in the "Start in" textbox;
2. Copy `tensorflow.dll` file from either the CPU or GPU [tensorflow release](https://www.tensorflow.org/install/lang_c#download_and_extract) to the `Extensions` folder;
3. If you are using TensorFlow GPU, make sure to add the `cuda/bin` folder of your cuDNN download to the `PATH` environment variable, or copy all DLL files to the `Extensions` folder.

## How to use

The core operator of Bonsai.DeepLabCut is the `DetectPose` node. You can place it after any image source, like so:

![detect-pose.svg](Resources/detect-pose.svg)

You will also need to point the `ModelFileName` to the exported .pb file containing your pretrained DLC model, and the `PoseConfigFileName` to the `pose_cfg.yaml` file describing the joint labels of the pose skeleton.

If everything works out, you should see some indications in the Bonsai command line window about whether the GPU was successfully detected and enabled. The first frame will cold start the inference graph which may take a bit of time, but after that your poses should start streaming through!

## Short DLC install guide

For all questions regarding installation of DeepLabCut, please check the official [docs](https://github.com/DeepLabCut/DeepLabCut/blob/master/docs/installation.md). However, we did find the following build steps to be reliable for a self-contained barebones install on Windows at the time of writing:

1. Download WinPython 3.8.10 64bit only from [GitHub](https://github.com/winpython/winpython/releases/download/4.3.20210620/Winpython64-3.8.10.0dot.exe).
2. Extract to a folder and launch `WinPython Command Prompt.exe`.
3. `pip install tensorflow==2.8.0` or `pip install tensorflow-gpu==2.8.0` depending on whether you will be using CPU or GPU TensorFlow.
4. `pip install deeplabcut[gui]==2.2.0.6`.
5. Launch the DLC gui: `python -m deeplabcut`.