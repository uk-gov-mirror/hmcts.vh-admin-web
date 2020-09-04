﻿using System;

namespace AdminWebsite.Models
{
    public class EditEndpointRequest
    {
        /// <summary>
        ///     Endpoint Id.
        /// </summary>
        public Guid? Id { get; set; }
        /// <summary>
        ///     The display name for the endpoint
        /// </summary>
        public string DisplayName { get; set; }
    }
}