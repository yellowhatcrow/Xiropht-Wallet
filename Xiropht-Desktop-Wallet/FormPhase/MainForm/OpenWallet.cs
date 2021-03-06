﻿using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MetroFramework;
using Xiropht_Connector_All.Setting;
using Xiropht_Connector_All.Utils;
using Xiropht_Connector_All.Wallet;
using Xiropht_Wallet.Features;
using Xiropht_Wallet.Wallet.Tcp;

#if WINDOWS
#endif

namespace Xiropht_Wallet.FormPhase.MainForm
{
    public partial class OpenWallet : Form
    {
        public string _fileSelectedPath;
        private string _walletFileData;

        public OpenWallet()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle = cp.ExStyle | 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }

        private void ButtonSearchWalletFile_Click(object sender, EventArgs e)
        {
            var openWalletFile = new OpenFileDialog
            {
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Filter = "Xiropht Wallet (*.xir) | *.xir",
                FilterIndex = 2,
                DereferenceLinks = false
            };
            if (openWalletFile.ShowDialog() == DialogResult.OK)
            {
                var threadReadWalletFileData = new Thread(delegate()
                {
                    _fileSelectedPath = openWalletFile.FileName;
                    labelOpenFileSelected.BeginInvoke((MethodInvoker) delegate
                    {
                        labelOpenFileSelected.Text =
                            ClassTranslation.GetLanguageTextFromOrder("OPEN_WALLET_LABEL_FILE_SELECTED_TEXT") +
                            " " + openWalletFile.FileName;
                    });
                    try
                    {
                        var streamReaderWalletFile = new StreamReader(openWalletFile.FileName);
                        _walletFileData = streamReaderWalletFile.ReadToEnd();
                        streamReaderWalletFile.Close();
                        Program.WalletXiropht.ClassWalletObject.WalletLastPathFile = openWalletFile.FileName;
                    }
                    catch
                    {
                    }
                });
                threadReadWalletFileData.Start();
            }
        }

        private void ButtonOpenYourWallet_Click(object sender, EventArgs e)
        {
            OpenAndConnectWallet();
        }

        /// <summary>
        ///     Open and connect the wallet.
        /// </summary>
        /// <returns></returns>
        private void OpenAndConnectWallet()
        {
            if (textBoxPasswordWallet.Text == "")
            {
#if WINDOWS
                ClassFormPhase.MessageBoxInterface(
                    ClassTranslation.GetLanguageTextFromOrder(
                        "OPEN_WALLET_ERROR_MESSAGE_NO_PASSWORD_WRITTED_CONTENT_TEXT"),
                    ClassTranslation.GetLanguageTextFromOrder(
                        "OPEN_WALLET_ERROR_MESSAGE_NO_PASSWORD_WRITTED_TITLE_TEXT"), MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
#else
                MessageBox.Show(Program.WalletXiropht, ClassTranslation.GetLanguageTextFromOrder("OPEN_WALLET_ERROR_MESSAGE_NO_PASSWORD_WRITTED_CONTENT_TEXT"),
                    ClassTranslation.GetLanguageTextFromOrder("OPEN_WALLET_ERROR_MESSAGE_NO_PASSWORD_WRITTED_TITLE_TEXT"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
#endif
                return;
            }

            if (Program.WalletXiropht.WalletEnableProxyMode)
            {
                if (!Program.WalletXiropht.EnableTokenNetworkMode)
                {
#if WINDOWS
                    if (MetroMessageBox.Show(Program.WalletXiropht,
                            "The proxy mode option is enabled, default mode to connect is recommended, also the proxy mode check process on initialization can take time. Do you want to continue ?",
                            "Proxy feature", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) ==
                        DialogResult.No)
                    {
                        Program.WalletXiropht.ClassWalletObject.FullDisconnection(true, true).ConfigureAwait(false);
                        return;
                    }
#else
                    if (MessageBox.Show(Program.WalletXiropht,
                            "The proxy mode option is enabled, default mode to connect is recommended, also the proxy mode check process on initialization can take time. Do you want to continue ?",
                            "Proxy feature", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) ==
                        DialogResult.No)
                    {
                        Program.WalletXiropht.ClassWalletObject.FullDisconnection(true, true).ConfigureAwait(false);
                        return;
                    }
#endif

                }
            }

            Task.Factory.StartNew(async delegate
            {

                await Program.WalletXiropht.InitializationWalletObject();
                try
                {
                    var error = false;

                    var passwordEncrypted = ClassAlgo.GetEncryptedResultManual(ClassAlgoEnumeration.Rijndael,
                        textBoxPasswordWallet.Text, textBoxPasswordWallet.Text, ClassWalletNetworkSetting.KeySize);

                    if (Program.WalletXiropht.ClassWalletObject != null)
                    {
                        Program.WalletXiropht.ClassWalletObject.WalletDataDecrypted =
                            ClassAlgo.GetDecryptedResultManual(
                                ClassAlgoEnumeration.Rijndael,
                                _walletFileData, passwordEncrypted, ClassWalletNetworkSetting.KeySize); // AES
                        if (Program.WalletXiropht.ClassWalletObject.WalletDataDecrypted ==
                            ClassAlgoErrorEnumeration.AlgoError) error = true;

                        if (error)
                            Program.WalletXiropht.ClassWalletObject.WalletDataDecrypted =
                                ClassAlgo.GetDecryptedResultManual(ClassAlgoEnumeration.Rijndael,
                                    _walletFileData, textBoxPasswordWallet.Text,
                                    ClassWalletNetworkSetting.KeySize); // AES

                        if (Program.WalletXiropht.ClassWalletObject.WalletDataDecrypted ==
                            ClassAlgoErrorEnumeration.AlgoError)
                        {
#if WINDOWS
                            ClassFormPhase.MessageBoxInterface(
                                ClassTranslation.GetLanguageTextFromOrder(
                                    "OPEN_WALLET_ERROR_MESSAGE_WRONG_PASSWORD_WRITTED_CONTENT_TEXT"),
                                ClassTranslation.GetLanguageTextFromOrder(
                                    "OPEN_WALLET_ERROR_MESSAGE_WRONG_PASSWORD_WRITTED_TITLE_TEXT"),
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
#else
                    MessageBox.Show(Program.WalletXiropht,
                        ClassTranslation.GetLanguageTextFromOrder("OPEN_WALLET_ERROR_MESSAGE_WRONG_PASSWORD_WRITTED_CONTENT_TEXT"),
                        ClassTranslation.GetLanguageTextFromOrder("OPEN_WALLET_ERROR_MESSAGE_WRONG_PASSWORD_WRITTED_TITLE_TEXT"), MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
                            return;
                        }
                    
                        var splitWalletFileDecrypted =
                            Program.WalletXiropht.ClassWalletObject.WalletDataDecrypted.Split(new[] {"\n"},
                                StringSplitOptions.None);
                        var walletAddress = splitWalletFileDecrypted[0];
                        var walletKey = splitWalletFileDecrypted[1];

                        if (Program.WalletXiropht.ClassWalletObject == null)
                        {
                            await Program.WalletXiropht.InitializationWalletObject();
                        }
                        if (!Program.WalletXiropht.EnableTokenNetworkMode)
                        {

                            if (!await Program.WalletXiropht.ClassWalletObject.InitializationWalletConnection(
                                walletAddress,
                                textBoxPasswordWallet.Text,
                                walletKey, ClassWalletPhase.Login))
                            {
                                MethodInvoker invoker = () => textBoxPasswordWallet.Text = "";
                                BeginInvoke(invoker);
#if WINDOWS
                                ClassFormPhase.MessageBoxInterface(
                                    ClassTranslation.GetLanguageTextFromOrder(
                                        "OPEN_WALLET_ERROR_MESSAGE_NETWORK_CONTENT_TEXT"),
                                    ClassTranslation.GetLanguageTextFromOrder(
                                        "OPEN_WALLET_ERROR_MESSAGE_NETWORK_TITLE_TEXT"), MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
#else
                        MessageBox.Show(Program.WalletXiropht,
                            ClassTranslation.GetLanguageTextFromOrder("OPEN_WALLET_ERROR_MESSAGE_NETWORK_CONTENT_TEXT"), ClassTranslation.GetLanguageTextFromOrder("OPEN_WALLET_ERROR_MESSAGE_NETWORK_TITLE_TEXT"), MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
#endif
                                await Program.WalletXiropht.ClassWalletObject.FullDisconnection(true, true);
                                return;
                            }

                            MethodInvoker invoke = () => textBoxPasswordWallet.Text = "";
                            BeginInvoke(invoke);

                            Program.WalletXiropht.ClassWalletObject.ListenSeedNodeNetworkForWallet();

                            _walletFileData = string.Empty;
                            _fileSelectedPath = string.Empty;
                            invoke = () =>
                                labelOpenFileSelected.Text =
                                    ClassTranslation.GetLanguageTextFromOrder(
                                        "OPEN_WALLET_LABEL_FILE_SELECTED_TEXT");
                            BeginInvoke(invoke);

                            if (Program.WalletXiropht.WalletSyncMode == ClassWalletSyncMode.WALLET_SYNC_DEFAULT || !Program.WalletXiropht.WalletEnableProxyMode)
                            {
                                if (await Program.WalletXiropht.ClassWalletObject.WalletConnect.SendPacketWallet(
                                    Program.WalletXiropht.ClassWalletObject.Certificate, string.Empty, false))
                                {
                                    await Program.WalletXiropht.ClassWalletObject.WalletConnect.SendPacketWallet(
                                        ClassConnectorSettingEnumeration.WalletLoginType + ClassConnectorSetting.PacketContentSeperator + Program.WalletXiropht
                                            .ClassWalletObject.WalletConnect.WalletAddress,
                                        Program.WalletXiropht.ClassWalletObject.Certificate, true);
                                }
                            }
                            else if (Program.WalletXiropht.WalletSyncMode ==
                                     ClassWalletSyncMode.WALLET_SYNC_PUBLIC_NODE && Program.WalletXiropht.WalletEnableProxyMode)
                            {
                                if (!ClassConnectorSetting.SeedNodeIp.ContainsKey(Program.WalletXiropht
                                    .ClassWalletObject.SeedNodeConnectorWallet.ReturnCurrentSeedNodeHost()))
                                {
                                    await Program.WalletXiropht.ClassWalletObject.SeedNodeConnectorWallet.SendPacketToSeedNodeAsync(
                                        ClassConnectorSettingEnumeration.WalletLoginProxy +
                                        ClassConnectorSetting.PacketContentSeperator +
                                        Program.WalletXiropht.ClassWalletObject.WalletConnect.WalletAddress +
                                        ClassConnectorSetting.PacketContentSeperator +
                                        Program.WalletXiropht.ClassWalletObject.Certificate + ClassConnectorSetting.PacketSplitSeperator, string.Empty, false, false);
                                }
                                else
                                {
                                    if (await Program.WalletXiropht.ClassWalletObject.WalletConnect.SendPacketWallet(
                                        Program.WalletXiropht.ClassWalletObject.Certificate, string.Empty, false))
                                    {
                                        await Program.WalletXiropht.ClassWalletObject.WalletConnect.SendPacketWallet(
                                            ClassConnectorSettingEnumeration.WalletLoginType + ClassConnectorSetting.PacketContentSeperator + Program.WalletXiropht
                                                .ClassWalletObject.WalletConnect.WalletAddress,
                                            Program.WalletXiropht.ClassWalletObject.Certificate, true);
                                    }
                                }

                            }
                            else if (Program.WalletXiropht.WalletSyncMode ==
                                     ClassWalletSyncMode.WALLET_SYNC_MANUAL_NODE && Program.WalletXiropht.WalletEnableProxyMode)
                            {
                                await Program.WalletXiropht.ClassWalletObject.SeedNodeConnectorWallet.SendPacketToSeedNodeAsync(
                                    ClassConnectorSettingEnumeration.WalletLoginProxy +
                                    ClassConnectorSetting.PacketContentSeperator +
                                    Program.WalletXiropht.ClassWalletObject.WalletConnect.WalletAddress +
                                    ClassConnectorSetting.PacketContentSeperator +
                                    Program.WalletXiropht.ClassWalletObject.Certificate + ClassConnectorSetting.PacketSplitSeperator, string.Empty, false, false);
                            }
                        }
                        else
                        {
                            Program.WalletXiropht.ClassWalletObject.InitializationWalletTokenMode(walletAddress,
                                walletKey,
                                textBoxPasswordWallet.Text);
                            MethodInvoker invoke = () => textBoxPasswordWallet.Text = "";
                            BeginInvoke(invoke);
                            invoke = () =>
                                labelOpenFileSelected.Text =
                                    ClassTranslation.GetLanguageTextFromOrder(
                                        "OPEN_WALLET_LABEL_FILE_SELECTED_TEXT");
                            BeginInvoke(invoke);
                            ClassFormPhase.SwitchFormPhase(ClassFormPhaseEnumeration.Overview);
                        }
                    }
                    else
                    {
#if WINDOWS
                        ClassFormPhase.MessageBoxInterface(
                            ClassTranslation.GetLanguageTextFromOrder(
                                "OPEN_WALLET_ERROR_MESSAGE_NETWORK_WRONG_PASSWORD_WRITTED_CONTENT_TEXT"),
                            ClassTranslation.GetLanguageTextFromOrder(
                                "OPEN_WALLET_ERROR_MESSAGE_NETWORK_WRONG_PASSWORD_WRITTED_TITLE_TEXT"),
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
#else
                MessageBox.Show(Program.WalletXiropht,
                    ClassTranslation.GetLanguageTextFromOrder("OPEN_WALLET_ERROR_MESSAGE_NETWORK_WRONG_PASSWORD_WRITTED_CONTENT_TEXT"),
                    ClassTranslation.GetLanguageTextFromOrder("OPEN_WALLET_ERROR_MESSAGE_NETWORK_WRONG_PASSWORD_WRITTED_TITLE_TEXT"), MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
                    }
                }
                catch
                {
#if WINDOWS
                    ClassFormPhase.MessageBoxInterface(
                        ClassTranslation.GetLanguageTextFromOrder(
                            "OPEN_WALLET_ERROR_MESSAGE_NETWORK_WRONG_PASSWORD_WRITTED_CONTENT_TEXT"),
                        ClassTranslation.GetLanguageTextFromOrder(
                            "OPEN_WALLET_ERROR_MESSAGE_NETWORK_WRONG_PASSWORD_WRITTED_TITLE_TEXT"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
#else
                MessageBox.Show(Program.WalletXiropht,
                    ClassTranslation.GetLanguageTextFromOrder("OPEN_WALLET_ERROR_MESSAGE_NETWORK_WRONG_PASSWORD_WRITTED_CONTENT_TEXT"),
                    ClassTranslation.GetLanguageTextFromOrder("OPEN_WALLET_ERROR_MESSAGE_NETWORK_WRONG_PASSWORD_WRITTED_TITLE_TEXT"), MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
                }
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Current);
        }

        /// <summary>
        ///     Get each control of the interface.
        /// </summary>
        public void GetListControl()
        {
            if (Program.WalletXiropht.ListControlSizeOpenWallet.Count == 0)
                for (var i = 0; i < Controls.Count; i++)
                    if (i < Controls.Count)
                        Program.WalletXiropht.ListControlSizeOpenWallet.Add(
                            new Tuple<Size, Point>(Controls[i].Size, Controls[i].Location));
        }

        private void OpenWallet_Load(object sender, EventArgs e)
        {
            UpdateStyles();
            Program.WalletXiropht.ResizeWalletInterface();
        }

        private void OpenWallet_Resize(object sender, EventArgs e)
        {
            UpdateStyles();
        }

        private void TextBoxPasswordWallet_KeyDownAsync(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) // Open wallet on press enter key.
                OpenAndConnectWallet();
        }

        private void checkBoxEnableTokenMode_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxEnableTokenMode.Checked)
                Program.WalletXiropht.EnableTokenNetworkMode = true;
            else
                Program.WalletXiropht.EnableTokenNetworkMode = false;
        }

        private void buttonTokenNetworkHelp_Click(object sender, EventArgs e)
        {
#if WINDOWS
            ClassFormPhase.MessageBoxInterface("The token network option permit to not have to use the online mode, and then to use only once this is necessary few requests to retrieve automatically your balance and to send a transaction once you want."+Environment.NewLine+"Note: The Token Network mode not permit to change your wallet password and not permit to disable your pin code.",
                "Token Network Option", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
#else
            MessageBox.Show(Program.WalletXiropht,"The token network option permit to not have to use the online mode, and then to use only once this is necessary few requests to retrieve automatically your balance and to send a transaction once you want."+Environment.NewLine+"Note: The Token Network mode not permit to change your wallet password and not permit to disable your pin code.",
                "Token Network Option", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
#endif
        }
    }
}