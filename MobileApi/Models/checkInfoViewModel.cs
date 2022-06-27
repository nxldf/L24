﻿using DataLayer.Enitities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MobileApi.Models
{
    public class checkInfoViewModel
    {
        public string userId { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string address { get; set; }
        public int country { get; set; }
        public string city { get; set; }
        public string zipcode { get; set; }
        public string PhoneNumber { get; set; }
        public string OS { get; set; }
        public PaymentMethod paymentMethod { get; set; }
    }
}