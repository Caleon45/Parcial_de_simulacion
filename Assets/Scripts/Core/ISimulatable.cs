namespace BattleRoyaleSim
{
    public interface ISimulatable
    {
        string EntityName { get; }
        bool IsActive { get; }
        void Simulate(float deltaTime);
        void ResetEntity();
    }
}

// Con apoyo de Gemini
