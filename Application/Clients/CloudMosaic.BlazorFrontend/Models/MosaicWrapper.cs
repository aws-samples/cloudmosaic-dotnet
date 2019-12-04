using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CloudMosaic.API.Client;

namespace CloudMosaic.BlazorFrontend.Models
{
    public class MosaicWrapper : INotifyPropertyChanged
    {
        public MosaicWrapper(MosaicSummary mosaic)
        {
            this.Mosaic = mosaic;
            this._status = this.Mosaic.Status.ToString();
        }
        public MosaicSummary Mosaic { get; set; }

        public string MosaicId => Mosaic.MosaicId;
        public string Name => Mosaic.Name;
        public DateTimeOffset CreateDate => Mosaic.CreateDate;
        public string MosaicThumbnailUrl => Mosaic.MosaicThumbnailUrl;
        public string MosaicFullUrl => Mosaic.MosaicFullUrl;
        public string MosaicWebPageSizeUrl => Mosaic.MosaicWebPageSizeUrl;

        string _status;
        public string Status
        {
            get 
            { 
                if(this.Mosaic.Status == MosaicStatuses.Failed)
                {
                    return $"Failed: {this.Mosaic.ErrorMessage}";
                }
                return this._status; 
            }
            set
            {
                this._status = value;
                OnPropertyChanged("Status");
            }
        }


        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
