/// <summary>
/// Guarda o resultado da partida entre a troca de cenas (Cena 3 → Cena 4), já que campos normais de
/// MonoBehaviour não sobrevivem ao carregamento de uma nova cena. O Scene4EndGameController lê esses
/// valores estáticos no Start().
/// </summary>
public static class GameResultData
{
    public static bool PlayerWon;
    public static int FinalScore;
    public static int CouponsCollected;
}
