import re

with open('d:/Unity/Projects/tooClose/Assets/Scripts/Inventory.cs', 'r', encoding='utf-8') as f:
    content = f.read()

old_block = '''        if (DieManagerUI.instance != null)
        {
            DieManagerUI.instance.UpdateDoubledRewards(addedStarsLastDie);
        }'''

new_block = '''        if (UIManager.instance != null)
        {
            UIManager.instance.Home();
        }'''

content = content.replace(old_block, new_block)

with open('d:/Unity/Projects/tooClose/Assets/Scripts/Inventory.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print('Inventory.cs updated.')
