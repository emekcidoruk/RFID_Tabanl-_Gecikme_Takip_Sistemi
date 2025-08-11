using System;
using System.Threading;
using System.IO;
using Org.LLRP.LTK.LLRPV1;
using Org.LLRP.LTK.LLRPV1.Impinj;
using Org.LLRP.LTK.LLRPV1.DataType;

namespace DocSample5
{
    class Program
    {
        static UInt32 msgID = 0;

        static int reportCount = 0;
        static int eventCount = 0;
        static int opSpecCount = 0;
        static readonly Random m_random = new();

        // command line parsing fills out these configurations
        static ENUM_ImpinjSerializedTIDMode m_tid = ENUM_ImpinjSerializedTIDMode.Disabled;
        static ENUM_ImpinjQTAccessRange m_shortRange = ENUM_ImpinjQTAccessRange.Normal_Range;
        static UInt32 m_password = 0;
        static UInt32 m_newPassword= 0;
        static UInt32 m_Verbose = 0;
        static UInt32 m_qtmode = 0;
        static string m_readerName = "unknown";

        // Simple Handler for receiving the tag reports from the reader
        static void Reader_OnRoAccessReportReceived(MSG_RO_ACCESS_REPORT msg)
        {
            // Report could be empty
            if (msg.TagReportData != null)
            {
                // Loop through and print out each tag
                for (int i = 0; i < msg.TagReportData.Length; i++)
                {
                    reportCount++;

                    // just write out the EPC as a hex string for now. It is guaranteed to be
                    // in all LLRP reports regardless of default configuration
                    string data = "\nEPC: ";
                    if (msg.TagReportData[i].EPCParameter[0].GetType() == typeof(PARAM_EPC_96))
                    {
                        data += ((PARAM_EPC_96)(msg.TagReportData[i].EPCParameter[0])).EPC.ToHexString();
                    }
                    else
                    {
                        data += ((PARAM_EPCData)(msg.TagReportData[i].EPCParameter[0])).EPC.ToHexString();
                    }

                    #region CheckForAccessResults
                    // check for Standard and Impinj OpSpecResults and print them out
                    if ((msg.TagReportData[i].AccessCommandOpSpecResult != null))
                    {
                        for(int x = 0; x < msg.TagReportData[i].AccessCommandOpSpecResult.Count; x++)
                        {
                            // it had better be the read result
                            if (msg.TagReportData[i].AccessCommandOpSpecResult[x].GetType()
                                == typeof(PARAM_C1G2ReadOpSpecResult))
                            {
                                PARAM_C1G2ReadOpSpecResult read = 
                                    (PARAM_C1G2ReadOpSpecResult)msg.TagReportData[i].AccessCommandOpSpecResult[x];
                                data += "\n    ReadResult(" + read.OpSpecID.ToString() + "): " + read.Result.ToString();
                                opSpecCount++;
                                if (read.Result == ENUM_C1G2ReadResultType.Success)
                                {
                                    data += "\n      Data: " + read.ReadData.ToHexWordString();
                                }
                            }
                            // it had better be the read result
                            if (msg.TagReportData[i].AccessCommandOpSpecResult[x].GetType()
                                == typeof(PARAM_C1G2WriteOpSpecResult))
                            {
                                PARAM_C1G2WriteOpSpecResult write = 
                                    (PARAM_C1G2WriteOpSpecResult)msg.TagReportData[i].AccessCommandOpSpecResult[x];
                                data += "\n    WriteResult(" + write.OpSpecID.ToString() + "): " + write.Result.ToString();
                                opSpecCount++;
                            }
                            if (msg.TagReportData[i].AccessCommandOpSpecResult[x].GetType() ==
                                typeof(PARAM_ImpinjGetQTConfigOpSpecResult))
                            {
                                PARAM_ImpinjGetQTConfigOpSpecResult get =
                                    (PARAM_ImpinjGetQTConfigOpSpecResult)msg.TagReportData[i].AccessCommandOpSpecResult[x];

                                opSpecCount++;
                                data += "\n    getQTResult(" + get.OpSpecID.ToString() + "): " + get.Result.ToString();
                                if (get.Result == ENUM_ImpinjGetQTConfigResultType.Success)
                                {
                                    data += "\n      Range :" + get.AccessRange.ToString();
                                    data += "\n      Profile: " + get.DataProfile.ToString();
                                }
                            }
                            if (msg.TagReportData[i].AccessCommandOpSpecResult[x].GetType() ==
                                typeof(PARAM_ImpinjSetQTConfigOpSpecResult))
                            {
                                PARAM_ImpinjSetQTConfigOpSpecResult set =
                                    (PARAM_ImpinjSetQTConfigOpSpecResult)msg.TagReportData[i].AccessCommandOpSpecResult[x];
                                opSpecCount++;
                                data += "\n    setQTResult(" + set.OpSpecID.ToString() + "): " + set.Result.ToString();
                            }
                        }
                    }
                    #endregion

                    #region CheckForImpinjCustomTagData
                    // check for other Impinj Specific tag data and print it out 
                    if (msg.TagReportData[i].Custom != null)
                    {
                        opSpecCount++;
                        for (int x = 0; x < msg.TagReportData[i].Custom.Count; x++)
                        {

                            if (msg.TagReportData[i].Custom[x].GetType() ==
                                typeof(PARAM_ImpinjSerializedTID))
                            {
                                PARAM_ImpinjSerializedTID stid =
                                    (PARAM_ImpinjSerializedTID)msg.TagReportData[i].Custom[x];
                                opSpecCount++;
                                data += "\n    serialTID: " + stid.TID.ToHexWordString();
                            }
                        }
                    }
                    #endregion

                    Console.WriteLine(data);
                }
            }
        }

        // Simple Handler for receiving the reader events from the reader
        static void Reader_OnReaderEventNotification(MSG_READER_EVENT_NOTIFICATION msg)
        {
            // Events could be empty
            if (msg.ReaderEventNotificationData == null) return;

            // Just write out the LTK-XML for now
            eventCount++;
            // speedway readers always report UTC timestamp
            UNION_Timestamp t = msg.ReaderEventNotificationData.Timestamp;
            PARAM_UTCTimestamp ut = (PARAM_UTCTimestamp)t[0];
            double millis = (ut.Microseconds + 500) / 1000;

            // LLRP reports time in microseconds relative to the Unix Epoch
            DateTime epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime now = epoch.AddMilliseconds(millis);

            Console.WriteLine("======Reader Event " + eventCount.ToString() + "======" +
                now.ToString("O"));

            // this is how you would look for individual events of interest
            // Here I just dump the event
            if (msg.ReaderEventNotificationData.AISpecEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.AISpecEvent.ToString());
            if (msg.ReaderEventNotificationData.AntennaEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.AntennaEvent.ToString());
            if (msg.ReaderEventNotificationData.ConnectionAttemptEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ConnectionAttemptEvent.ToString());
            if (msg.ReaderEventNotificationData.ConnectionCloseEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ConnectionCloseEvent.ToString());
            if (msg.ReaderEventNotificationData.GPIEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.GPIEvent.ToString());
            if (msg.ReaderEventNotificationData.HoppingEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.HoppingEvent.ToString());
            if (msg.ReaderEventNotificationData.ReaderExceptionEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ReaderExceptionEvent.ToString());
            if (msg.ReaderEventNotificationData.ReportBufferLevelWarningEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ReportBufferLevelWarningEvent.ToString());
            if (msg.ReaderEventNotificationData.ReportBufferOverflowErrorEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ReportBufferOverflowErrorEvent.ToString());
            if (msg.ReaderEventNotificationData.ROSpecEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ROSpecEvent.ToString());
        }

        // displays the usage information for this command 
        static void Usage()
        {
            Console.WriteLine("Usage: docSample5.exe [options] READERHOSTNAME");
            Console.WriteLine("     -p <password> -- specify an optional password for operations");
            Console.WriteLine("     -n <password> -- specifies a new password for the set password command");
            Console.WriteLine("     -t  -- specify to automatically backscatter the TID");
            Console.WriteLine("     -s  -- if setting QT config, -s will short range the tag");
            Console.WriteLine("     -q <n>  -- run QT scenario n where n is defined as ");
            Console.WriteLine("         0 -- Read standard TID memory");
            Console.WriteLine("         1 -- set tag password (uses -p, -n )");
            Console.WriteLine("         2 -- Read private memory data without QT commands");
            Console.WriteLine("         3 -- read QT status of tag (uses -p)");
            Console.WriteLine("         4 -- set QT status of tag to private (uses -p, -s)");
            Console.WriteLine("         5 -- set QT status of tag to public (uses -p, -s)");
            Console.WriteLine("         6 -- Peek at private memory data with temporary QT command (uses -p)");
            Console.WriteLine("         7 -- Write 32 words of user data to random values");
            Console.WriteLine("         8 -- Write 6 words of public EPC data to random values");
            Console.WriteLine("         9 -- Read Reserved memory");
            Console.WriteLine("");
            return;
        }

        static void Main(string[] args)
        {
            LLRPClient reader;
            int i;

            #region ProcessCommandLine 
            if (args.Length < 1)
            {
                Usage();
                return;
            }

            // get the options. Skip the last one as its the hostname
            for( i = 0; i < args.Length-1 ; i++)
            {
                if((args[i] == "-p") && (i < (args.Length-1)))
                {
                    i++;
                    m_password = System.Convert.ToUInt32(args[i]);
                }
                else if ((args[i] == "-n") && (i < (args.Length - 1)))
                {
                    i++;
                    m_newPassword = System.Convert.ToUInt32(args[i]);    
                }        
                else if (args[i] == "-t")
                {
                    m_tid = ENUM_ImpinjSerializedTIDMode.Enabled;
                }
                else if (args[i] == "-s") 
                {
                    m_shortRange = ENUM_ImpinjQTAccessRange.Short_Range;
                }
                else if ((args[i] == "-v") && (i < (args.Length - 1)))
                {
                    i++;
                    m_Verbose = System.Convert.ToUInt32(args[i]);
                }
                else if ((args[i] == "-q") && (i < (args.Length - 1)))
                {
                    i++;
                    m_qtmode = System.Convert.ToUInt32(args[i]);
                }   
                else
                {
                    Usage();
                    return;
                }
            }

            m_readerName = args[i];

            Console.WriteLine(
                "Impinj C# LTK.NET RFID Application DocSample5 reader - " +
                m_readerName + "\n");

            Console.WriteLine(
                " qtMode:" + m_qtmode.ToString() +
                " Verbose:" + m_Verbose.ToString() +
                " Range:" + m_shortRange.ToString() +
                " SerializeTID:" + m_tid.ToString() +
                " OldPassword:" + m_password.ToString() +
                " NewPassword:" + m_newPassword.ToString());
            #endregion

            #region Initializing
            {
                Console.WriteLine("Initializing\n");

                // Create an instance of LLRP reader client.
                reader = new LLRPClient();

                // Impinj Best Practice! Always Install the Impinj extensions
                Impinj_Installer.Install();
            }
            #endregion

            #region EventHandlers
            {
                Console.WriteLine("Adding Event Handlers\n");
                reader.OnReaderEventNotification += new delegateReaderEventNotification(Reader_OnReaderEventNotification);
                reader.OnRoAccessReportReceived += new delegateRoAccessReport(Reader_OnRoAccessReportReceived);
            }
            #endregion

            #region Connecting
            {
                Console.WriteLine("Connecting To Reader\n");

                // Open the reader connection.  Timeout after 5 seconds
                bool ret = reader.Open(m_readerName, 5000, out ENUM_ConnectionAttemptStatusType status);

                // Ensure that the open succeeded and that the reader
                // returned the correct connection status result
                if (!ret || status != ENUM_ConnectionAttemptStatusType.Success)
                {
                    Console.WriteLine("Failed to Connect to Reader \n");
                    return;
                }
            }
            #endregion

            #region EnableExtensions
            {
                Console.WriteLine("Enabling Impinj Extensions\n");

                MSG_IMPINJ_ENABLE_EXTENSIONS imp_msg = new()
                {
                    MSG_ID = msgID++  // note: this doesn't need to be set as the library will default
                };

                // Send the custom message and wait for 8 seconds
                MSG_CUSTOM_MESSAGE cust_rsp = reader.CUSTOM_MESSAGE(imp_msg, out MSG_ERROR_MESSAGE msg_err, 8000);

                if (cust_rsp is MSG_IMPINJ_ENABLE_EXTENSIONS_RESPONSE msg_rsp)
                {
                    if (msg_rsp.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(msg_rsp.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("Enable Extensions Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region FactoryDefault
            {
                Console.WriteLine("Factory Default the Reader\n");

                // factory default the reader
                MSG_SET_READER_CONFIG msg_cfg = new()
                {
                    ResetToFactoryDefault = true,
                    MSG_ID = msgID++  // this doesn't need to be set as the library will default

                };

                // if SET_READER_CONFIG affects antennas it could take several seconds to return
                MSG_SET_READER_CONFIG_RESPONSE rsp_cfg = reader.SET_READER_CONFIG(msg_cfg, out MSG_ERROR_MESSAGE msg_err, 12000);

                if (rsp_cfg != null)
                {
                    if (rsp_cfg.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(rsp_cfg.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("SET_READER_CONFIG Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region getReaderCapabilities
            {
                Console.WriteLine("Getting Reader Capabilities\n");

                MSG_GET_READER_CAPABILITIES cap = new()
                {
                    MSG_ID = msgID++,
                    RequestedData = ENUM_GetReaderCapabilitiesRequestedData.All
                };

                // Send the custom message and wait for 8 seconds
                MSG_GET_READER_CAPABILITIES_RESPONSE msg_rsp =
                          reader.GET_READER_CAPABILITIES(cap, out MSG_ERROR_MESSAGE msg_err, 8000);

                if (msg_rsp != null)
                {
                    if (msg_rsp.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(msg_rsp.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("GET reader Capabilities Command Timed out\n");
                    reader.Close();
                    return;
                }

                // Get the reader model number since some features are not
                // available on Speedway revolution.
                PARAM_GeneralDeviceCapabilities dev_cap = msg_rsp.GeneralDeviceCapabilities;

                // Check to make sure the model number matches and that this device
                // is an impinj reader (deviceManufacturerName == 25882)
                if ((dev_cap == null) ||
                    (dev_cap.DeviceManufacturerName != 25882))
                {
                    Console.WriteLine("Could not determine reader model number\n");
                    reader.Close();
                    return;
                }
                
                // Need to parse version number strings and compare to make sure
                // that the reader version is higher than 4.4.
                Version readerVersion = new(dev_cap.ReaderFirmwareVersion);
                Version minimumVersion = new("4.4.0.0");

                if (readerVersion < minimumVersion)
                {
                    Console.WriteLine("Must use Octane 4.4 or later\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region SetReaderConfig
            {
                Console.WriteLine("Adding SET_READER_CONFIG n");

                // Communicate that message to the reader
                MSG_SET_READER_CONFIG msg = new()
                {
                    MSG_ID = msgID++,
                    ResetToFactoryDefault = false,
                    // turn off all reports 
                    ROReportSpec = new PARAM_ROReportSpec()
                    {
                        TagReportContentSelector = new PARAM_TagReportContentSelector
                        {
                            EnableAccessSpecID = false,
                            EnableAntennaID = false,
                            EnableChannelIndex = false,
                            EnableFirstSeenTimestamp = false,
                            EnableInventoryParameterSpecID = false,
                            EnableLastSeenTimestamp = false,
                            EnablePeakRSSI = false,
                            EnableROSpecID = false,
                            EnableSpecIndex = false,
                            EnableTagSeenCount = false
                        },
                        // report all tags immediately
                        ROReportTrigger = ENUM_ROReportTriggerType.Upon_N_Tags_Or_End_Of_ROSpec,
                        N = 1
                    }
                };

                // turn on serialized TID if we are asked to
                PARAM_ImpinjTagReportContentSelector impinjTagData = new()
                {
                    ImpinjEnableGPSCoordinates = new PARAM_ImpinjEnableGPSCoordinates()
                    {
                        GPSCoordinatesMode = ENUM_ImpinjGPSCoordinatesMode.Disabled
                    },
                    ImpinjEnablePeakRSSI = new PARAM_ImpinjEnablePeakRSSI
                    {
                        PeakRSSIMode = ENUM_ImpinjPeakRSSIMode.Disabled
                    },
                    ImpinjEnableRFPhaseAngle = new PARAM_ImpinjEnableRFPhaseAngle
                    {
                        RFPhaseAngleMode = ENUM_ImpinjRFPhaseAngleMode.Disabled
                    },
                    ImpinjEnableSerializedTID = new PARAM_ImpinjEnableSerializedTID
                    {
                        SerializedTIDMode = m_tid
                    }
                };
                msg.ROReportSpec.Custom.Add(impinjTagData);

                // report access specs immediately as well
                msg.AccessReportSpec = new PARAM_AccessReportSpec
                {
                    AccessReportTrigger = ENUM_AccessReportTriggerType.End_Of_AccessSpec
                };

                // set the antenna configuration for all antennas 
                msg.AntennaConfiguration = new PARAM_AntennaConfiguration[1];
                msg.AntennaConfiguration[0] = new PARAM_AntennaConfiguration
                {
                    AntennaID = 0 // all antennas
                };

                // use DRM autset mode 
                PARAM_C1G2InventoryCommand c1g2Inv = new()
                {
                    C1G2RFControl = new PARAM_C1G2RFControl
                    {
                        ModeIndex = 1000,
                        Tari = 0
                    },

                    // Use session 1 so we don't get too many reads 
                    C1G2SingulationControl = new PARAM_C1G2SingulationControl()
                    {
                        Session = new TwoBits(1),
                        TagPopulation = 1,
                        TagTransitTime = 0
                    }
                };

                // add to the message 
                msg.AntennaConfiguration[0].AirProtocolInventoryCommandSettings.Add(c1g2Inv);

                MSG_SET_READER_CONFIG_RESPONSE rsp = reader.SET_READER_CONFIG(msg, out MSG_ERROR_MESSAGE msg_err, 12000);
                if (rsp != null)
                {
                    if (rsp.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(rsp.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("SET_READER_CONFIG Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region ADDRoSpecWithXML
            {
                Console.WriteLine("Adding RoSpec from XML file \n");

                Org.LLRP.LTK.LLRPV1.DataType.Message obj;

                // read the XML from a file and validate it is an ADD_ROSPEC
                try
                {
                    string filename;
                    filename = @"..\..\..\addRoSpec.xml";
                    FileStream fs = new(filename, FileMode.Open);
                    StreamReader sr = new(fs);
                    string s = sr.ReadToEnd();
                    fs.Close();

                    LLRPXmlParser.ParseXMLToLLRPMessage(s, out obj, out ENUM_LLRP_MSG_TYPE msg_type);

                    if (obj == null || msg_type != ENUM_LLRP_MSG_TYPE.ADD_ROSPEC)
                    {
                        Console.WriteLine("Could not extract message from XML");
                        reader.Close();
                        return;
                    }
                }
                catch
                {
                    Console.WriteLine("Unable to convert to valid XML");
                    reader.Close();
                    return;
                }

                MSG_ADD_ROSPEC msg = (MSG_ADD_ROSPEC)obj;
                msg.MSG_ID = msgID++;

                // Communicate that message to the reader
                MSG_ADD_ROSPEC_RESPONSE rsp = reader.ADD_ROSPEC(msg, out MSG_ERROR_MESSAGE msg_err, 12000);
                if (rsp != null)
                {
                    if (rsp.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(rsp.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("ADD_ROSPEC Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region ADDAccessSpec
            {
                // This section adds a second accessSpec identical to the
                // first (except for its ID).  This is duplicate code with
                // the goal of showing an example of how to build LLRP specs
                // from C# objects rather than XML
                Console.WriteLine("Adding AccessSpec from C# objects \n");

                // create the target tag filter spec to perform access only on these tags
                // This only requires a single filter (LTK/LLRP supports up to 2)
                PARAM_C1G2TargetTag[] targetTag = new PARAM_C1G2TargetTag[1];
                targetTag[0] = new PARAM_C1G2TargetTag
                {
                    Match = true,
                    MB = new TwoBits(1),
                    Pointer = 16,
                    TagData = LLRPBitArray.FromHexString(""),
                    TagMask = LLRPBitArray.FromHexString("")
                };

                PARAM_C1G2TagSpec tagSpec = new()
                {
                    C1G2TargetTag = targetTag
                };

                PARAM_AccessCommand accessCmd = new()
                {
                    AirProtocolTagSpec = new UNION_AirProtocolTagSpec()
                };
                accessCmd.AirProtocolTagSpec.Add(tagSpec);

                switch(m_qtmode)
                {
                    case 0:
                        PARAM_C1G2Read readStdTID = new()
                        {
                            AccessPassword = 0,
                            MB = new TwoBits(2),
                            OpSpecID = 1,
                            WordCount = 2,
                            WordPointer = 0
                        };
                        accessCmd.AccessCommandOpSpec.Add(readStdTID);
                        break;

                    case 1:
                        PARAM_C1G2Write writePassword = new()
                        {
                            OpSpecID = 2,
                            MB = new TwoBits(0),
                            AccessPassword = m_password,
                            WordPointer = 2,
                            WriteData = new UInt16Array()
                        };
                        writePassword.WriteData.Add( (UInt16) ((m_newPassword >> 16) & 0x0000ffff));
                        writePassword.WriteData.Add( (UInt16) (m_newPassword & 0x0000ffff));
                        accessCmd.AccessCommandOpSpec.Add(writePassword);
                        break;

                    case 2:
                        PARAM_C1G2Read readSerializedTID = new()
                        {
                            AccessPassword = 0,
                            MB = new TwoBits(2),
                            OpSpecID = 3,
                            WordCount = 6,
                            WordPointer = 0
                        };
                        accessCmd.AccessCommandOpSpec.Add(readSerializedTID);

                        PARAM_C1G2Read readPublicEPC = new()
                        {
                            AccessPassword = 0,
                            MB = new TwoBits(2),
                            OpSpecID = 4,
                            WordCount = 6,
                            WordPointer = 6
                        };
                        accessCmd.AccessCommandOpSpec.Add(readPublicEPC);

                        PARAM_C1G2Read readUser = new()
                        {
                            AccessPassword = 0,
                            MB = new TwoBits(3),
                            OpSpecID = 5,
                            WordCount = 32,
                            WordPointer = 0
                        };
                        accessCmd.AccessCommandOpSpec.Add(readUser);
                        break;                        

                    case 3:
                        PARAM_ImpinjGetQTConfig getQT = new()
                        {
                            OpSpecID = 6,
                            AccessPassword = m_password
                        };
                        accessCmd.AccessCommandOpSpec.Add(getQT);
                        break;
                    case 4:
                        PARAM_ImpinjSetQTConfig setQTPrivate = new()
                        {
                            OpSpecID = 7,
                            AccessPassword = m_password,
                            Persistence = ENUM_ImpinjQTPersistence.Permanent,
                            DataProfile = ENUM_ImpinjQTDataProfile.Private,
                            AccessRange = m_shortRange
                        };
                        accessCmd.AccessCommandOpSpec.Add(setQTPrivate);
                        break;
                    case 5:
                        PARAM_ImpinjSetQTConfig setQTPublic = new()
                        {
                            OpSpecID = 8,
                            AccessPassword = m_password,
                            Persistence = ENUM_ImpinjQTPersistence.Permanent,
                            DataProfile = ENUM_ImpinjQTDataProfile.Public,
                            AccessRange = m_shortRange
                        };
                        accessCmd.AccessCommandOpSpec.Add(setQTPublic);
                        break;
                    case 6:
                        PARAM_ImpinjSetQTConfig setQTPeek = new()
                        {
                            OpSpecID = 9,
                            AccessPassword = m_password,
                            Persistence = ENUM_ImpinjQTPersistence.Temporary,
                            DataProfile = ENUM_ImpinjQTDataProfile.Private,
                            AccessRange = ENUM_ImpinjQTAccessRange.Normal_Range
                        };
                        accessCmd.AccessCommandOpSpec.Add(setQTPeek);

                        PARAM_C1G2Read readSerializedTIDPeek = new()
                        {
                            AccessPassword = 0,
                            MB = new TwoBits(2),
                            OpSpecID = 10,
                            WordCount = 6,
                            WordPointer = 0
                        };
                        accessCmd.AccessCommandOpSpec.Add(readSerializedTIDPeek);

                        PARAM_C1G2Read readPrivateEPC = new()
                        {
                            AccessPassword = 0,
                            MB = new TwoBits(1),
                            OpSpecID = 11,
                            WordCount = 8,
                            WordPointer = 2
                        };
                        accessCmd.AccessCommandOpSpec.Add(readPrivateEPC);

                        PARAM_C1G2Read readUserPeek = new()
                        {
                            AccessPassword = 0,
                            MB = new TwoBits(3),
                            OpSpecID = 12,
                            WordCount = 32,
                            WordPointer = 0
                        };
                        accessCmd.AccessCommandOpSpec.Add(readUserPeek);
                        break;
                    case 7:
                        PARAM_C1G2Write writeUser = new()
                        {
                            AccessPassword = m_password,
                            OpSpecID = 13,
                            WordPointer = 0,
                            MB = new TwoBits(3),

                            WriteData = new UInt16Array()
                        };
                        for (int x = 0; x < 32; x++)
                        {
                            writeUser.WriteData.Add((UInt16)m_random.Next(65536));
                        }

                        accessCmd.AccessCommandOpSpec.Add(writeUser);
                        break;
                    case 8:
                        PARAM_C1G2Write writePubEPC = new()
                        {
                            AccessPassword = m_password,
                            MB = new TwoBits(2),
                            OpSpecID = 14,
                            WordPointer = 6,

                            WriteData = new UInt16Array()
                        };
                        for (int x = 0; x < 6; x++)
                        {
                            writePubEPC.WriteData.Add((UInt16)m_random.Next(65536));
                        }

                        accessCmd.AccessCommandOpSpec.Add(writePubEPC);
                        break;
                    case 9:
                        PARAM_C1G2Read readRsvd = new()
                        {
                            AccessPassword = m_password,
                            MB = new TwoBits(0),
                            OpSpecID = 15,
                            WordCount = 4,
                            WordPointer = 0
                        };
                        accessCmd.AccessCommandOpSpec.Add(readRsvd);
                        break;
                }

                // create the stop trigger for the Access Spec
                PARAM_AccessSpecStopTrigger stop = new()
                {
                    AccessSpecStopTrigger = ENUM_AccessSpecStopTriggerType.Null,
                    OperationCountValue = 0
                };

                // Create and set up the basic accessSpec
                PARAM_AccessSpec accessSpec = new()
                {
                    AccessSpecID = 24,
                    AntennaID = 0,
                    ROSpecID = 0,
                    CurrentState = ENUM_AccessSpecState.Disabled,
                    ProtocolID = ENUM_AirProtocols.EPCGlobalClass1Gen2,

                    // add the access command and stop trigger to the accessSpec
                    AccessCommand = accessCmd,
                    AccessSpecStopTrigger = stop
                };

                // Add the Access Spec to the ADD_ACCESSSPEC message
                MSG_ADD_ACCESSSPEC addAccess = new()
                {
                    MSG_ID = msgID++,
                    AccessSpec = accessSpec
                };

                // communicate the message to the reader
                MSG_ADD_ACCESSSPEC_RESPONSE rsp = reader.ADD_ACCESSSPEC(addAccess, out MSG_ERROR_MESSAGE msg_err, 12000);
                if (rsp != null)
                {
                    if (rsp.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(rsp.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("ADD_ACCESSSPEC Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region EnableAccessSpec
            {
                Console.WriteLine("Enabling AccessSpec\n");
                MSG_ENABLE_ACCESSSPEC msg = new()
                {
                    MSG_ID = msgID++,
                    AccessSpecID = 24 // this better match the ACCESSSPEC we created above
                };
                MSG_ENABLE_ACCESSSPEC_RESPONSE rsp = reader.ENABLE_ACCESSSPEC(msg, out MSG_ERROR_MESSAGE msg_err, 12000);
                if (rsp != null)
                {
                    if (rsp.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(rsp.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("ENABLE_ACCESSSPEC Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region EnableRoSpec
            {
                Console.WriteLine("Enabling RoSpec\n");
                MSG_ENABLE_ROSPEC msg = new()
                {
                    MSG_ID = msgID++,
                    ROSpecID = 1111 // this better match the ROSpec we created above
                };
                MSG_ENABLE_ROSPEC_RESPONSE rsp = reader.ENABLE_ROSPEC(msg, out MSG_ERROR_MESSAGE msg_err, 12000);
                if (rsp != null)
                {
                    if (rsp.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(rsp.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("ENABLE_ROSPEC Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region StartRoSpec
            {
                Console.WriteLine("Starting RoSpec\n");
                MSG_START_ROSPEC msg = new()
                {
                    MSG_ID = msgID++,
                    ROSpecID = 1111 // this better match the RoSpec we created above
                };
                MSG_START_ROSPEC_RESPONSE rsp = reader.START_ROSPEC(msg, out MSG_ERROR_MESSAGE msg_err, 12000);
                if (rsp != null)
                {
                    if (rsp.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(rsp.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("START_ROSPEC Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            // this should be plenty long enough to do these commands
            Thread.Sleep(3000);

            #region StopRoSpec
            {
                Console.WriteLine("Stopping RoSpec\n");
                MSG_STOP_ROSPEC msg = new()
                {
                    ROSpecID = 1111 // this better match the RoSpec we created above
                };
                MSG_STOP_ROSPEC_RESPONSE rsp = reader.STOP_ROSPEC(msg, out MSG_ERROR_MESSAGE msg_err, 12000);
                if (rsp != null)
                {
                    if (rsp.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(rsp.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("STOP_ROSPEC Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region Clean Up Reader Configuration
            {
                Console.WriteLine("Factory Default the Reader\n");

                // factory default the reader
                MSG_SET_READER_CONFIG msg_cfg = new()
                {
                    ResetToFactoryDefault = true,
                    MSG_ID = msgID++ // note this doesn't need to be set as the library will default
                };

                // Note that if SET_READER_CONFIG affects antennas it could take several seconds to return
                MSG_SET_READER_CONFIG_RESPONSE rsp_cfg = reader.SET_READER_CONFIG(msg_cfg, out MSG_ERROR_MESSAGE msg_err, 12000);

                if (rsp_cfg != null)
                {
                    if (rsp_cfg.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(rsp_cfg.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("SET_READER_CONFIG Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            Console.WriteLine("  Received " + opSpecCount + " OpSpec Results.");
            Console.WriteLine("  Received " + reportCount + " Tag Reports.");
            Console.WriteLine("  Received " + eventCount + " Events.");
            Console.WriteLine("Closing\n");
            // clean up the reader
            reader.Close();
            reader.OnReaderEventNotification -= new delegateReaderEventNotification(Reader_OnReaderEventNotification);
            reader.OnRoAccessReportReceived -= new delegateRoAccessReport(Reader_OnRoAccessReportReceived);
        }
    }
}
