using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharepointMessenger.Models
{
    public class Contact
    {
        private string _imageUrl = "";
        public string ImageUrl
        {
            get { return _imageUrl; }
            set { _imageUrl = value; }
        }
        private int _id;
        public int ID
        {
            get { return _id; }
            set { _id = value; }
        }
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        private string _username;
        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }
    }
}
