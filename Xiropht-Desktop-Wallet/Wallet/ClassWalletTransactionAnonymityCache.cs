﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xiropht_Connector_All.Remote;
using Xiropht_Connector_All.Setting;
using Xiropht_Connector_All.Utils;
using Xiropht_Connector_All.Wallet;
using Xiropht_Wallet.FormPhase;

namespace Xiropht_Wallet.Wallet
{
    public class ClassWalletTransactionAnonymityCache
    {
        private const string WalletTransactionCacheDirectory = "/Cache/";
        private const string WalletTransactionCacheFileExtension = "transaction.xirtra";
        private static bool _inClearCache;
        public static Dictionary<long, string> ListTransaction;

        /// <summary>
        /// Load transaction in cache.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <returns></returns>
        public static void LoadWalletCache(string walletAddress)
        {
            if (ListTransaction != null)
            {
                ListTransaction.Clear();
            }
            else
            {
                ListTransaction = new Dictionary<long, string>();
            }
            if (!string.IsNullOrEmpty(walletAddress))
            {
                walletAddress += "ANONYMITY";

                if (Directory.Exists(
                    ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory + walletAddress + "\\")))
                {
                    if (File.Exists(ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory + walletAddress + "\\" + WalletTransactionCacheFileExtension)))
                    {
                        using (FileStream fs = File.Open(ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory + walletAddress + "\\" + WalletTransactionCacheFileExtension), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (BufferedStream bs = new BufferedStream(fs))
                        using (StreamReader sr = new StreamReader(bs))
                        {
                            string line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                if (!ListTransaction.ContainsValue(line))
                                {
                                    ListTransaction.Add(ListTransaction.Count, line);
                                }
                            }
                        }
                    }
                    else
                    {
                        File.Create(ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory + walletAddress + "\\" + WalletTransactionCacheFileExtension)).Close();
                    }

                }
                else
                {
                    if (Directory.Exists(ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory)) == false)
                    {
                        Directory.CreateDirectory(ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory));
                    }

                    Directory.CreateDirectory(ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory +
                                              walletAddress));
                }
            }
        }

        /// <summary>
        /// Save each transaction into cache
        /// </summary>
        /// <param name="walletAddress"></param>
        public static void SaveWalletCache(string walletAddress, string transaction)
        {
            if (!string.IsNullOrEmpty(walletAddress))
            {
                walletAddress += "ANONYMITY";
                if (ListTransaction.Count > 0 && _inClearCache == false)
                {
                    if (Directory.Exists(ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory)) == false)
                    {
                        Directory.CreateDirectory(ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory));
                    }

                    if (Directory.Exists(ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory +
                                         walletAddress)) == false)
                    {
                        Directory.CreateDirectory(ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory +
                                                  walletAddress));
                    }


                    if (!File.Exists(ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory +
                                    walletAddress +
                                    "\\" + WalletTransactionCacheFileExtension)))
                    {
                        File.Create(ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory + walletAddress + "\\" + WalletTransactionCacheFileExtension)).Close();
                    }
                    try
                    {

                        using (var transactionFile = new StreamWriter(ClassUtility.ConvertPath(
                            System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory +
                            walletAddress +
                            "\\" + WalletTransactionCacheFileExtension), true))
                        {
                            transactionFile.WriteLine(transaction);
                        }
                    }
                    catch
                    {
                        // ignored
                    }

                }
            }
        }

        /// <summary>
        /// Clear each transaction into cache.
        /// </summary>
        /// <param name="walletAddress"></param>
        public static bool RemoveWalletCache(string walletAddress)
        {
            _inClearCache = true;
            if (!string.IsNullOrEmpty(walletAddress))
            {
                walletAddress += "ANONYMITY";
                if (Directory.Exists(
                    ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory + walletAddress + "\\")))
                {
                    if (File.Exists(ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory + walletAddress + "\\" + WalletTransactionCacheFileExtension)))
                    {
                        File.Delete(ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory + walletAddress + "\\" + WalletTransactionCacheFileExtension));
                    }

                    Directory.Delete(
                        ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory + walletAddress + "\\"), true);
                    Directory.CreateDirectory(ClassUtility.ConvertPath(System.AppDomain.CurrentDomain.BaseDirectory + WalletTransactionCacheDirectory +
                                              walletAddress));
                }

                ListTransaction.Clear();
            }
            _inClearCache = false;
            return true;
        }

        /// <summary>
        /// Add transaction to the list.
        /// </summary>
        /// <param name="transaction"></param>
        public static void AddWalletTransaction(string transaction)
        {
            try
            {
#if DEBUG
                Log.WriteLine("Wallet transaction history received: " + transaction
                                       .Replace(
                                           ClassRemoteNodeCommandForWallet.RemoteNodeRecvPacketEnumeration
                                               .WalletTransactionPerId, ""));
#endif
                var splitTransaction = transaction.Replace(
                             ClassRemoteNodeCommandForWallet.RemoteNodeRecvPacketEnumeration.WalletAnonymityTransactionPerId,
                             "").Split(new[] { "#" }, StringSplitOptions.None);
                var type = splitTransaction[0];
                var timestamp = splitTransaction[3]; // Timestamp Send CEST.
                var hashTransaction = splitTransaction[4]; // Transaction Hash.
                var realFeeAmountSend = splitTransaction[7]; // Real fee and amount crypted for sender.
                var realFeeAmountRecv = splitTransaction[8]; // Real fee and amount crypted for sender.


                var decryptKey =
                         ClassWalletObject.WalletConnect.WalletAddress + ClassWalletObject.WalletConnect.WalletKey; // Wallet Address + Wallet Public Key

                var amountAndFeeDecrypted = "NULL";
                if (type == "SEND")
                    amountAndFeeDecrypted = ClassAlgo.GetDecryptedResultManual(ClassAlgoEnumeration.Rijndael,
                            realFeeAmountSend, decryptKey, ClassWalletNetworkSetting.KeySize); // AES
                else if (type == "RECV")
                    amountAndFeeDecrypted = ClassAlgo.GetDecryptedResultManual(ClassAlgoEnumeration.Rijndael,
                            realFeeAmountRecv, decryptKey, ClassWalletNetworkSetting.KeySize); // AES

                if (amountAndFeeDecrypted != "NULL" && amountAndFeeDecrypted != ClassAlgoErrorEnumeration.AlgoError)
                {
                    var splitDecryptedAmountAndFee =
                        amountAndFeeDecrypted.Split(new[] { "-" }, StringSplitOptions.None);
                    var amountDecrypted = splitDecryptedAmountAndFee[0];
                    var feeDecrypted = splitDecryptedAmountAndFee[1];
                    var walletDstOrSrc = splitDecryptedAmountAndFee[2];


                    var timestampRecv = splitTransaction[5]; // Timestamp Recv CEST.
                    var blockchainHeight = splitTransaction[6]; // Blockchain height.

                    var finalTransaction = type + "#" + hashTransaction + "#" + walletDstOrSrc + "#" +
                                                amountDecrypted + "#" + feeDecrypted + "#" + timestamp + "#" +
                                                timestampRecv + "#" + blockchainHeight;

                    var finalTransactionEncrypted =
                         ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael, finalTransaction,
                            ClassWalletObject.WalletConnect.WalletAddress + ClassWalletObject.WalletConnect.WalletKey,
                            ClassWalletNetworkSetting.KeySize); // AES

                    if (finalTransactionEncrypted == ClassAlgoErrorEnumeration.AlgoError) // Ban bad remote node.
                    {
                        if (!ClassConnectorSetting.SeedNodeIp.ContainsKey(ClassWalletObject.ListWalletConnectToRemoteNode[8].RemoteNodeHost))
                        {
                            if (!ClassWalletObject.ListRemoteNodeBanned.ContainsKey(ClassWalletObject.ListWalletConnectToRemoteNode[8].RemoteNodeHost))
                            {
                                ClassWalletObject.ListRemoteNodeBanned.Add(ClassWalletObject.ListWalletConnectToRemoteNode[8].RemoteNodeHost, ClassUtils.DateUnixTimeNowSecond());
                            }
                            else
                            {
                                ClassWalletObject.ListRemoteNodeBanned[ClassWalletObject.ListWalletConnectToRemoteNode[8].RemoteNodeHost] = ClassUtils.DateUnixTimeNowSecond();
                            }
                        }
                        ClassWalletObject.DisconnectWholeRemoteNodeSync(true, true);
                    }
                    else
                    {

                        if (!ListTransaction.ContainsValue(finalTransactionEncrypted))
                        {

                            ListTransaction.Add(ListTransaction.Count, finalTransactionEncrypted);


                            SaveWalletCache(ClassWalletObject.WalletConnect.WalletAddress, finalTransactionEncrypted);

#if DEBUG
                            Log.WriteLine("Total transactions downloaded: " +
                                               ListTransaction.Count + "/" +
                                               ClassWalletObject.TotalTransactionInSync + ".");
#endif
                        }

                    }
                }
                else
                {
#if DEBUG
                    Log.WriteLine("Can't decrypt transaction: " + transaction + " result: " +
                                  amountAndFeeDecrypted);
#endif
                    if (!ClassConnectorSetting.SeedNodeIp.ContainsKey(ClassWalletObject.ListWalletConnectToRemoteNode[8].RemoteNodeHost))
                    {
                        if (!ClassWalletObject.ListRemoteNodeBanned.ContainsKey(ClassWalletObject.ListWalletConnectToRemoteNode[8].RemoteNodeHost))
                        {
                            ClassWalletObject.ListRemoteNodeBanned.Add(ClassWalletObject.ListWalletConnectToRemoteNode[8].RemoteNodeHost, ClassUtils.DateUnixTimeNowSecond());
                        }
                        else
                        {
                            ClassWalletObject.ListRemoteNodeBanned[ClassWalletObject.ListWalletConnectToRemoteNode[8].RemoteNodeHost] = ClassUtils.DateUnixTimeNowSecond();
                        }
                    }
                    ClassWalletObject.DisconnectWholeRemoteNodeSync(true, true);
                }
            }
            catch
            {

            }
            ClassWalletObject.InReceiveTransactionAnonymity = false;

        }
    }
}