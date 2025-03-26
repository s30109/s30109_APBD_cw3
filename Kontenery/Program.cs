using System;
using System.Collections.Generic;
using System.Linq;

namespace ContainerManagement
{
    public class OverfillException : Exception
    {
        public OverfillException(string message) : base(message) { }
    }
    
    public interface IIsDangerous
    {
        void DangerousAlert(string message);
    }
    
    public static class GenSerNum
    {
        private static int count = 0;
        public static string GenSer(string typeOfCon)
        {
            count++;
            return $"KONTENER->{typeOfCon}->{count}";
        }
    }

    public abstract class Container
    {
        public double Weight { get; protected set; }
        public double Height { get; protected set; }
        public double OwnWeight { get; protected set; }
        public double Depth { get; protected set; }
        public string SerNum { get; protected set; }
        public double MaxCapacity { get; protected set; }

        public Container(double height, double ownWeight, double depth, double maxCapacity, string typeOfCon)
        {
            Weight = 0;
            Height = height;
            OwnWeight = ownWeight;
            Depth = depth;
            MaxCapacity = maxCapacity;
            SerNum = GenSerNum.GenSer(typeOfCon);
        }

        public virtual void Load(double mass)
        {
            if (mass + Weight > MaxCapacity)
                throw new OverfillException($"proba zaladowania za duzej ilosci w kontenerze o numerze -> {SerNum}");
            Weight += mass;
        }

        public virtual void Unload()
        {
            Weight = 0;
        }

        public override string ToString()
        {
            return $"Numer Seryjny -> {SerNum}, Waga -> {Weight} kg, Pojemnosc -> {MaxCapacity} kg";
        }
    }

    public class LiquidContainer : Container, IIsDangerous
    {
        public LiquidContainer(double height, double ownWeight, double depth, double maxCapacity)
            : base(height, ownWeight, depth, maxCapacity, "L")
        { }

        public void DangerousAlert(string message)
        {
            Console.WriteLine($"[Niebezpieczenstwo] {message} w kontenerze -> {SerNum}");
        }

        public void LoadCargo(double mass, bool isHazardous)
        {
            double limit = isHazardous ? 0.5 * MaxCapacity : 0.9 * MaxCapacity;
            if (mass + Weight > limit)
            {
                DangerousAlert("ladunek przekroczyl limit");
                throw new OverfillException($"proba zaladowania za duzej ilosci w kontenerze o numerze -> {SerNum}");
            }
            Load(mass);
        }
    }

    public class GasContainer : Container, IIsDangerous
    {
        public double Pressure { get; private set; }

        public GasContainer(double height, double ownWeight, double depth, double maxCapacity, double pressure)
            : base(height, ownWeight, depth, maxCapacity, "G")
        {
            Pressure = pressure;
        }

        public void DangerousAlert(string message)
        {
            Console.WriteLine($"[Niebezpieczenstwo] {message} w kontenerze -> {SerNum}");
        }

        public override void Load(double mass)
        {
            if (mass + Weight > MaxCapacity)
            {
                DangerousAlert("ladunek przekroczyl limit");
                throw new OverfillException($"proba zaladowania za duzej ilosci w kontenerze o numerze -> {SerNum}");
            }
            base.Load(mass);
        }

        public override void Unload()
        {
            Weight *= 0.05;
            DangerousAlert("rozladowyanie -> zostawiono 5% ladunku");
        }
    }

    public class RefrigeratedContainer : Container
    {
        public string ProductType { get; private set; }
        public double Temperature { get; private set; } 
        public double RequiredTemperature { get; private set; } 

        public RefrigeratedContainer(double height, double ownWeight, double depth, double maxCapacity,
                                     string productType, double requiredTemperature, double temperature)
            : base(height, ownWeight, depth, maxCapacity, "C")
        {
            ProductType = productType;
            RequiredTemperature = requiredTemperature;
            Temperature = temperature;
            if (Temperature < RequiredTemperature)
                throw new Exception($"Temperatura kontenera -> ({Temperature}°C) jest nizsza niż wymagana dla produktu -> {ProductType} wymaga -> ({RequiredTemperature}°C)");
        }
    }

    public class ContainerShip
    {
        public List<Container> Containers { get; private set; }
        public double MaxSpeed { get; private set; } 
        public int MaxContainers { get; private set; }
        public double MaxTotalWeight { get; private set; } 

        public ContainerShip(double maxSpeed, int maxContainers, double maxTotalWeight)
        {
            MaxSpeed = maxSpeed;
            MaxContainers = maxContainers;
            MaxTotalWeight = maxTotalWeight;
            Containers = new List<Container>();
        }

        public double CurrentTotalWeight()
        {
            double totalKg = Containers.Sum(c => c.OwnWeight + c.Weight);
            return totalKg / 1000.0;
        }

        public void AddContainer(Container container)
        {
            if (Containers.Count >= MaxContainers)
            {
                Console.WriteLine("nie mozna dodac kontenera -> przekroczono maksymalna liczbe");
                return;
            }
            if (CurrentTotalWeight() + ((container.OwnWeight + container.Weight) / 1000.0) > MaxTotalWeight)
            {
                Console.WriteLine("nie mozna dodac kontenera -> przekroczono maksymalna wage");
                return;
            }
            Containers.Add(container);
            Console.WriteLine($"dodano kontener -> {container.SerNum} do statku");
        }

        public void AddContainers(IEnumerable<Container> containers)
        {
            foreach (var container in containers)
            {
                AddContainer(container);
            }
        }

        public void RemoveContainer(string SerNum)
        {
            var container = Containers.FirstOrDefault(c => c.SerNum == SerNum);
            if (container != null)
            {
                Containers.Remove(container);
                Console.WriteLine($"usunięto kontener -> {SerNum} ze statku");
            }
            else
            {
                Console.WriteLine($"nie znaleziono kontenera -> {SerNum} na statku");
            }
        }

        public void UnloadContainer(string SerNum)
        {
            var container = Containers.FirstOrDefault(c => c.SerNum == SerNum);
            if (container != null)
            {
                container.Unload();
                Console.WriteLine($"rozladowano kontener -> {SerNum}.");
            }
            else
            {
                Console.WriteLine($"nie znaleziono kontenera -> {SerNum} na statku");
            }
        }

        public void ReplaceContainer(string SerNum, Container newContainer)
        {
            var index = Containers.FindIndex(c => c.SerNum == SerNum);
            if (index != -1)
            {
                Containers[index] = newContainer;
                Console.WriteLine($"zastapiono kontener -> {SerNum} kontenerem -> {newContainer.SerNum}.");
            }
            else
            {
                Console.WriteLine($"nie znaleziono kontenera -> {SerNum} na statku");
            }
        }

        public void TransferContainer(string SerNum, ContainerShip destinationShip)
        {
            var container = Containers.FirstOrDefault(c => c.SerNum == SerNum);
            if (container != null)
            {
                RemoveContainer(SerNum);
                destinationShip.AddContainer(container);
                Console.WriteLine($"przeniesiono kontener -> {SerNum} na inny statek");
            }
            else
            {
                Console.WriteLine($"nie znaleziono kontenera -> {SerNum} na statku");
            }
        }

        public void PrintShipInfo()
        {
            Console.WriteLine("___ Informacje o statku ___");
            Console.WriteLine($"Maksymalna predkosc -> {MaxSpeed} wezlow");
            Console.WriteLine($"Maksymalna liczba kontenerow -> {MaxContainers}");
            Console.WriteLine($"Maksymalna waga kontenerow -> {MaxTotalWeight} ton");
            Console.WriteLine("Kontenery na statku:");
            foreach (var container in Containers)
            {
                Console.WriteLine($"  - {container}");
            }
            Console.WriteLine($"Aktualna laczna waga kontenerow: {CurrentTotalWeight()} ton");
        }
    }
    
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {

                LiquidContainer liquidContainer = new LiquidContainer(300, 2000, 250, 10000);
                liquidContainer.LoadCargo(8000, false);

                LiquidContainer hazardousLiquidContainer = new LiquidContainer(300, 2000, 250, 10000);
                hazardousLiquidContainer.LoadCargo(4500, true);

                GasContainer gasContainer = new GasContainer(300, 1800, 240, 8000, 2.5);
                gasContainer.Load(7000);
                gasContainer.Unload();

                RefrigeratedContainer refrigeratedContainer = new RefrigeratedContainer(300, 2200, 260, 9000, "mleko", 4, 5);
                refrigeratedContainer.Load(8500);

                ContainerShip ship1 = new ContainerShip(25, 10, 50);
                ship1.AddContainer(liquidContainer);
                ship1.AddContainer(hazardousLiquidContainer);
                ship1.AddContainer(gasContainer);
                ship1.AddContainer(refrigeratedContainer);

                ship1.PrintShipInfo();

                ship1.RemoveContainer(liquidContainer.SerNum);

                LiquidContainer newLiquidContainer = new LiquidContainer(300, 2000, 250, 10000);
                newLiquidContainer.LoadCargo(8500, false);
                ship1.ReplaceContainer(hazardousLiquidContainer.SerNum, newLiquidContainer);

                ContainerShip ship2 = new ContainerShip(30, 8, 40);
                ship1.TransferContainer(gasContainer.SerNum, ship2);

                Console.WriteLine("\n___ Statek 1 ___");
                ship1.PrintShipInfo();
                Console.WriteLine("\n___ Statek 2 ___");
                ship2.PrintShipInfo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"wystapil wyjatek: {ex.Message}");
            }
        }
    }
}
