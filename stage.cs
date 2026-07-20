using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace blitz
{
    public class stage : INotifyPropertyChanged
    {
        double _from;
        double _to;
        int _rank;
        int _tier;

        public double from
        {
            get => _from;
            set { _from = value; rise(); rise(nameof(text)); }
        }

        public double to
        {
            get => _to;
            set { _to = value; rise(); rise(nameof(text)); }
        }

        public int rank
        {
            get => _rank;
            set { _rank = value; rise(); rise(nameof(tint)); rise(nameof(face)); }
        }

        public int tier
        {
            get => _tier;
            set { _tier = value; rise(); rise(nameof(mark)); }
        }

        [JsonIgnore]
        public string text => $"{_from:0.#}% → {_to:0.#}%";

        [JsonIgnore]
        public string face => _rank == 0 ? "не начато" : _rank == 1 ? "в работе" : "пройден";

        [JsonIgnore]
        public Brush tint
        {
            get
            {
                if (_rank == 2) return (Brush)new BrushConverter().ConvertFromString("#2ea44f");
                if (_rank == 1) return (Brush)new BrushConverter().ConvertFromString("#4a90e2");
                return (Brush)new BrushConverter().ConvertFromString("#333336");
            }
        }

        [JsonIgnore]
        public string full => $"{_from:0.#} - {_to:0.#}";

        [JsonIgnore]
        public string mark => $"стейдж {_tier}";

        public void next()
        {
            rank = (rank + 1) % 3;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void rise([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
