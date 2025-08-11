////////////////////////////////////////////////////////////////////////////////
//
//    Read Enhanced Integra
//
////////////////////////////////////////////////////////////////////////////////

using System;
using Impinj.OctaneSdk;

namespace OctaneSdkExamples
{
    class Program
    {
        // Create an instance of the ImpinjReader class.
        static ImpinjReader reader = new ImpinjReader();

        static void Main(string[] args)
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

                // Assign the TagsReported event handler.
                // This specifies which method to call
                // when tags reports are available.
                reader.TagsReported += OnTagsReported;

                // Get the default settings. We'll use these as a 
                // starting point and then modify the settings 
                // we're interested in.
                Settings settings = reader.QueryDefaultSettings();

                // Tell the reader to include the antenna number
                // and Enhanced Integra reporting in all tag reports. 
                // Enhanced Integra is available on Impinj Monza 700 series and later chips.
                settings.Report.IncludeAntennaPortNumber = true;
                settings.Report.IncludeEnhancedIntegra = true;

                // Apply the newly modified settings.
                reader.ApplySettings(settings);

                // Start the reader.
                reader.Start();

                // Wait for the user to press enter.
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();

                // Stop reading.
                reader.Stop();

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
                Console.WriteLine("Exception : {0}", e.Message);
            }
        }

        static void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            // This event handler is called asynchronously 
            // when tag reports are available.
            // Loop through each tag in the report 
            // and print the data.
            foreach (Tag tag in report)
            {
                if (tag.IsEnhancedIntegraReportPresent)
                {
                    // Print the Enhanced Integra report data.
                    Console.WriteLine("Antenna : {0}\nEPC : {1}\nEnhanced Integra Result : {2}\n" +
                                       "Enhanced Integra Op Spec ID : {3}\n\n",
                                      tag.AntennaPortNumber,
                                      tag.Epc,
                                      tag.EnhancedIntegraResult,
                                      tag.EnhancedIntegraOpSpecId);
                }
                else
                {
                    // Enhanced Integra not available.
                    // Chip is not a Monza 700 series or later.
                    Console.WriteLine("Antenna : {0}\nEPC : {1}\nEnhanced Integra : Report not available\n\n",
                                      tag.AntennaPortNumber,
                                      tag.Epc);
                }
            }
        }
    }
}
