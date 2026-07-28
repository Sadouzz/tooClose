import re

with open('d:/Unity/Projects/tooClose/Assets/Scripts/AdMob.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Add DoubleRewards logic in ShowRewardedAd
if 'if (_reward == "DoubleRewards")' not in content:
    double_rewards_code = '''
                    if (_reward == "DoubleRewards")
                    {
                        if (Inventory.instance != null)
                        {
                            Inventory.instance.DoubleEndGameRewards();
                        }
                    }
'''
    # Find LifeRegen logic to insert right before it
    content = content.replace('if (_reward == "LifeRegen")', double_rewards_code.strip() + '\n                    if (_reward == "LifeRegen")')

with open('d:/Unity/Projects/tooClose/Assets/Scripts/AdMob.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print('AdMob.cs updated.')
