// <copyright file="ApplicationInfo.cs" company="Luminis BV">
// Copyright (c) Luminis BV. All rights reserved.
// </copyright>

namespace Luminis.AzureActiveDirectory.Models
{
    using Microsoft.Graph;

    /// <summary>
    /// The application info.
    /// </summary>
    public class ApplicationInfo
    {
        /// <summary>
        /// Gets or sets the homepage url.
        /// </summary>
        public string HomePageUrl { get; set; }

        /// <summary>
        /// Gets or sets the logo url.
        /// </summary>
        public string LogoUrl { get; set; }

        /// <summary>
        /// Gets or sets the privacystatement url.
        /// </summary>
        public string PrivacyStatementUrl { get; set; }

        /// <summary>
        /// Gets or sets the terms of service url.
        /// </summary>
        public string TermsOfServiceUrl { get; set; }

        /// <summary>
        /// Constructs a new Application Info object.
        /// </summary>
        /// <param name="application">The azure ad application.</param>
        public static explicit operator ApplicationInfo(Application application)
        {
            return new ApplicationInfo()
            {
                HomePageUrl = application.Web.HomePageUrl,
                LogoUrl = application.Info.LogoUrl,
                PrivacyStatementUrl = application.Info.PrivacyStatementUrl,
                TermsOfServiceUrl = application.Info.TermsOfServiceUrl,
            };
        }
    }
}
