import re

with open('d:/Unity/Projects/tooClose/Assets/Scripts/Inventory.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Add hasRevived variable
if 'public bool hasRevived = false;' not in content:
    content = content.replace('public bool inPlay = false;', 'public bool inPlay = false;\n    public bool hasRevived = false;')

# Reset hasRevived in ResetData
if 'hasRevived = false;' not in content:
    content = content.replace('public void ResetData()\n    {', 'public void ResetData()\n    {\n        hasRevived = false;')

# Set hasRevived to true in AdsReward
if 'hasRevived = true;' not in content:
    content = content.replace('public void AdsReward()\n    {', 'public void AdsReward()\n    {\n        hasRevived = true;')

# Add DoubleEndGameRewards if not present
if 'public void DoubleEndGameRewards()' not in content:
    double_rewards_code = '''
    public void DoubleEndGameRewards()
    {
        int currentTotal = PlayerPrefs.GetInt("stars", 0);
        PlayerPrefs.SetInt("stars", currentTotal + addedStarsLastDie);
        
        int totalDestroyedMissiles = PlayerPrefs.GetInt("totalDestroyedMissiles", 0);
        PlayerPrefs.SetInt("totalDestroyedMissiles", totalDestroyedMissiles + MissileSpawner.instance.destroyedMissiles);
        
        int totalDestroyedEnemies = PlayerPrefs.GetInt("totalDestroyedEnemies", 0);
        PlayerPrefs.SetInt("totalDestroyedEnemies", totalDestroyedEnemies + MissileSpawner.instance.destroyedEnemies);

        PlayerPrefs.Save();

        if (DieManagerUI.instance != null)
        {
            DieManagerUI.instance.UpdateDoubledRewards(addedStarsLastDie);
        }
    }
'''
    content = content.replace('public void ResetData()', double_rewards_code + '\n    public void ResetData()')

with open('d:/Unity/Projects/tooClose/Assets/Scripts/Inventory.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print('Inventory.cs updated.')
