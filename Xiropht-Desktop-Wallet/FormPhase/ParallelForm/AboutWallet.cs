﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using Xiropht_Wallet.Features;

namespace Xiropht_Wallet.FormPhase.ParallelForm
{
    public partial class AboutWallet : Form
    {
        public AboutWallet()
        {
            InitializeComponent();
        }

        private void About_Load(object sender, EventArgs e)
        {
            var contributorsText = string.Empty;
            if (ClassTranslation.LanguageContributors.Count > 0)
                foreach (var contributor in ClassTranslation.LanguageContributors)
                {
                    contributorsText += ClassTranslation.UppercaseFirst(contributor.Key) + ": ";
                    if (contributor.Value.Count > 0)
                        for (var i = 0; i < contributor.Value.Count; i++)
                            if (i < contributor.Value.Count)
                                contributorsText += contributor.Value[i] + "  ";
                    contributorsText += Environment.NewLine;
                }

            labelLanguageContributors.Text = contributorsText;

            labelWalletGuiVersion.Text = "Xiropht Desktop Wallet v" +
                                         Assembly.GetExecutingAssembly().GetName().Version + "R" + Environment.NewLine +
                                         "Copyright © " + DateTime.Now.Year + " Xiropht Developer";
        }

        private void linkLabelWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://xiropht.com/");
        }
    }
}