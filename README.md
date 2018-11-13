# PlanQA_Plugin
ESAPI Plugin for RT Plan QA

This project is an ESAPI Plugin for Radiation Therapy Plan QA. It is based on Varian Eclipse version 13.6 and has only been tested on 13.6. You can try it on other versions but some revision might be required.

Usage:

1. Download or clone the project.

2. Open the project using Visual Studio on a Eclipse workstation. VS Community Version will work.

3. Compile the project.

4. Copy the generated file PlanQA_Plugin.esapi.dll to the PublishedScripts folder on your Varian Image Server.

5. Copy the Protocol folder coming with this project to the same location.

6. You can start to test the plugin now: Open Plan in External Beam Planning --> Tools --> Scripts --> System Scripts --> Select the PlanQA_Plugin.esapi.dll script --> Run --> Select the protocol --> Open.

7. Results failed to meet the criteria will have be labeled with '??'.

8. If you are happy with this script, you can create your own protocol for QA based on the instructions in any of the protocol files coming with this project.

9. Enjoy your QA using the ESAPI script!


For users who just want to try the script and don't like to setup a Visual Studio environment, you can follow the steps below:

1. Download the compiled DLL file (based on Eclipse 13.6 using VS2017) in obj/x64/Debug/ to your Eclipse computer.

2. Download the QAProtocols folder to the same location where you save the DLL file.

3. Start your test: Open Plan in External Beam Planning --> Tools --> Scripts --> System Scripts --> Select the PlanQA_Plugin.esapi.dll script --> Run --> Select the protocol --> Open.

4. If you are OK with it, you can put it into your PublishedScripts folder on you image server so that everybody can use it.

You are welcome for any questions with this plugin!
