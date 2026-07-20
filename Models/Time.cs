using System;

namespace DeliveryApi.Models
{
    // 🚨 Objeto de valor: representa una duración en minutos
    public class Time
    {
        public int Minutes { get; }

        public Time(int minutes)
        {
            Minutes = minutes < 0 ? 0 : minutes;
        }

        public static Time FromDistance(double kilometers, double averageSpeedKmH)
        {
            int minutos = (int)Math.Ceiling((kilometers / averageSpeedKmH) * 60);
            return new Time(minutos);
        }

        public Time Add(Time other) => new Time(Minutes + other.Minutes);

        public string ToDisplayString() => $"{Minutes} min";
    }
}