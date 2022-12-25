using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordConsoleHost.Model
{
    public class Customer : INotifyPropertyChanged
    {

        public int id { get; set; }

        private ulong _channelId;
        public ulong ChannelId
        {
            get { return _channelId; }
            set { _channelId = value; OnPropertyChanged(nameof(ChannelId)); }
        }

        private ulong _customerId;
        public ulong CustomerId
        {
            get { return _customerId; }
            set { _customerId = value; OnPropertyChanged(nameof(CustomerId)); }
        }

        private string _customerName;
        public string CustomerName
        {
            get { return _customerName; }
            set { _customerName = value; OnPropertyChanged(nameof(CustomerName)); }
        }

        public void OnPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
