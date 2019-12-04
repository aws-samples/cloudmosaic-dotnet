using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CloudMosaic.API.Client;


namespace CloudMosaic.BlazorFrontend.Models
{
    public class GalleryWrapper : INotifyPropertyChanged
    {
        public Gallery Gallery { get; set; }

        public GalleryWrapper(Gallery gallery)
        {
            this.Gallery = gallery;
        }

        public string Name => Gallery.Name;

        public string GalleryId => Gallery.GalleryId;

        public bool IsPublic
        {
            get { return Gallery.Sharing == GallerySharingState.Public; }
            set
            {
                if (value)
                    Gallery.Sharing = GallerySharingState.Public;
                else
                    Gallery.Sharing = GallerySharingState.Private;
            }
        }

        public long TileCount => Gallery.TileCount;

        public DateTimeOffset CreateDate => Gallery.CreateDate;


        string _status;
        public string Status
        {
            get { return this._status; }
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
