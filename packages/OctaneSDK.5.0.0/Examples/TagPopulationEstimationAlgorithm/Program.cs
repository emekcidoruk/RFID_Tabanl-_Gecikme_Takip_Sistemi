using Impinj.OctaneSdk;
using System;
using System.Threading.Tasks;

namespace TagPopulationEstimationAlgorithm
{
    internal class Program
    {
        // Create an instance of the ImpinjReader class.
        static ImpinjReader reader = new ImpinjReader();

        static int s_readCount = 0;

        static async Task Main(string[] args)
        {
            try
            {
                // Connect to the reader.
                // Pass in a reader hostname or IP address as a 
                // command line argument when running the example
                if (args.Length != 1)
                {
                    Console.WriteLine("Error: No hostname specified.  Pass in the reader hostname as a command line argument when running the Sdk Example.");
                    return;
                }
                string hostname = args[0];
                reader.Connect(hostname);

                // Get the default settings
                // We'll use these as a starting point
                // and then modify the settings we're 
                // interested in.

                Settings settings = reader.QueryDefaultSettings();

                // If ReaderDefault is coming back from QueryDefaultSettings, the firmware doesn't support tag population
                // estimation and we can't run this example.
                if (settings.TagPopulationEstimationAlgorithm == TagPopulationEstimationMode.ReaderDefault)
                {
                    reader.Disconnect();
                    throw new Exception("This example can only be demonstrated on 8.0 firmware and above.");
                }

                // Tell the reader to include the antenna number
                // in all tag reports. Other fields can be added
                // to the reports in the same way by setting the 
                // appropriate Report.IncludeXXXXXXX property.
                settings.Report.IncludeAntennaPortNumber = true;

                // The reader can be set into various modes in which reader
                // dynamics are optimized for specific regions and environments.
                // The following mode, AutoSetDenseReaderDeepScan (1002), monitors RF noise
                // and interference then automatically and continuously optimizes
                // the reader’s configuration
                settings.RfMode = 1002;
                settings.SearchMode = SearchMode.DualTarget;
                settings.Session = 2;

                // This population estimate should be much too large 
                settings.TagPopulationEstimate = 1000;
                settings.TagPopulationEstimationAlgorithm = TagPopulationEstimationMode.Disabled;
                
                // Enable antenna #1. Disable all others.
                settings.Antennas.DisableAll();
                settings.Antennas.GetAntenna(1).IsEnabled = true;

                // Set the Transmit Power and 
                // Receive Sensitivity to the maximum.
                settings.Antennas.GetAntenna(1).MaxTxPower = true;
                settings.Antennas.GetAntenna(1).MaxRxSensitivity = true;

                // Apply the newly modified settings.
                reader.ApplySettings(settings);

                // Assign the TagsReported event handler.
                // This specifies which method to call
                // when tags reports are available.
                reader.TagsReported += OnTagsReported;

                // Start reading.
                reader.Start();

                // Run for 5 seconds
                await Task.Delay(5000);

                reader.Stop();

                // Let tag reads finish
                await Task.Delay(1000);

                int badEstimateReadCount = s_readCount;
                s_readCount = 0;

                settings.TagPopulationEstimationAlgorithm = TagPopulationEstimationMode.Enabled;

                reader.ApplySettings(settings);

                Console.WriteLine("Read count with user-provided tag population estimate: {0}", badEstimateReadCount);
                Console.WriteLine("Now applying Tag Population Estimation Algorithm");

                await Task.Delay(2000);

                reader.Start();

                await Task.Delay(5000);

                reader.Stop();

                // Let tag reads finish
                await Task.Delay(1000);

                int estimationAlgorithmReadCount = s_readCount;

                Console.WriteLine("Read count with user-provided tag population estimate: {0}", badEstimateReadCount);
                Console.WriteLine("Read count with tag population estimation algorithm: {0}", estimationAlgorithmReadCount);

                // Wait for the user to press enter.
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();

                // Disconnect from the reader.
                reader.Disconnect();
            }
            catch (OctaneSdkException e)
            {
                // Handle Octane SDK errors.
                Console.WriteLine("Octane SDK exception: {0}", e.Message);
            }
            catch (Exception e)
            {
                // Handle other .NET errors.
                Console.WriteLine("Exception: {0}", e.Message);
            }
        }

        static void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            // This event handler is called asynchronously 
            // when tag reports are available.
            // Default test for ReadTags 
            // Loop through each tag in the report 
            // and print the data.
            foreach (Tag tag in report)
            {
                Console.WriteLine("Antenna: {0}, EPC: {1} ",
                                tag.AntennaPortNumber, tag.Epc);
                s_readCount++;
            }
        }
    }
}
