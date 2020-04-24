# PlanQA_Plugin
ESAPI Plugin for RT Plan QA

This project is a ESAPI Plugin for Radiation Therapy Plan QA with different protocols.

Usage:

1. Download or clone the project.

2. Open the project using Visual Studio on a Eclipse workstation. VS Community Version will work.

3. Compile the project.

4. Copy the generated file PlanQA_Plugin.esapi.dll to the PublishedScripts folder on your Varian Image Server.

5. Copy the Protocol folder coming with this project to the same location.

6. You can start to test the plugin now: Open Plan in External Beam Planning --> Tools --> Scripts --> System Scripts --> Select the PlanQA_Plugin.esapi.dll script --> Run --> Select the protocol --> Open.

7. Results failed to meet the criteria will be labeled with a red 'X'.

8. If you are happy with this script, you can create your own protocol for QA based on the instructions in any of the protocol files coming with this project.

9. Enjoy your QA using the ESAPI script!


For users who just want to try the script and don't like to setup a Visual Studio environment, you can follow the steps below:

1. Download the compiled DLL file in obj/x64/Debug/ to your Eclipse computer.

2. Download the QAProtocols folder to the same location where you save the DLL file.

3. Start your test: Open Plan in External Beam Planning --> Tools --> Scripts --> System Scripts --> Select the PlanQA_Plugin.esapi.dll script --> Run --> Select the protocol --> Open.

4. If you are OK with it, you can put it into your PublishedScripts folder on you image server so that everybody can use it.

*Please use SABR_RTOG0915_4800_4.protocol as the sample and follow the instructions in the file to construct your own protocol. The other sample protocol files included in the same folder may not be up to date.*

*This Plugin is currently developed and tested with Eclipse 15.6. It may not work directly for other Eclipse versions (11.x, 13.x and 16.x). Some changes, should be minimal, might be required if you want to use it with other Eclipse versions.*

You are welcome for any questions with this program! I can be reached via email: FirstnameLastname AT GMAIL DOT COM.
