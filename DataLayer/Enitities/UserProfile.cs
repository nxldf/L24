﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Enitities
{
    public enum ProfileType { Artist, Collector, Agency }
    public enum IDCardStatus { None, Confirmed, Pending, NotConfirmed }
    public class UserProfile
    {
        [Key, ForeignKey("ApplicationUserDetail")]
        public virtual String Id { get; set; }
        public virtual ApplicationUser ApplicationUserDetail { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LegalName { get; set; }
        public virtual string LastName { get; set; }
        public bool AuctionCapability { get; set; }
        public virtual string PhotoPath { get; set; }
        [ForeignKey("country")]
        public int? countryId { get; set; }
        public virtual Country country { get; set; }
        public virtual string City { get; set; }
        public virtual string GovermentIdPath { get; set; }
        public virtual bool isIDConfirmed { get; set; }
        public virtual IDCardStatus IDStatus { get; set; }
        public virtual string IdConfirmedBy { get; set; }
        public virtual string IdRejectionReason { get; set; }
        public virtual string Region { get; set; }
        public virtual decimal Account { get; set; }
        public virtual string ZipCode { get; set; }
        public bool MailingList { get; set; }
        public bool ReceiveNewArtEmail { get; set; }
        public virtual PersonalInformation personalInformation { get; set; }
        public virtual UserLink userLinks { get; set; }
        public virtual BillingInfo billingInfo { get; set; }
        public virtual ProfileType profileType { get; set; }
        public virtual DateTime RegisterDate { get; set; }
        public virtual ICollection<Collection> Collections { get; set; }
        public virtual ICollection<Product> Products { get; set; }
        public virtual ICollection<Favorit> Favorits { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<PayoutRequest> PayoutRequests { get; set; }
        public virtual ICollection<Watch> Watches { get; set; }
        public virtual ICollection<Bid> Bids { get; set; }
        public UserProfile()
        {
            RegisterDate = DateTime.Now;
        }
    }
}
