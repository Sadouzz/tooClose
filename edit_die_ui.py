import re

with open('d:/Unity/Projects/tooClose/Assets/Scripts/DieManagerUI.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Add reviveButton reference
if 'public GameObject reviveButton;' not in content:
    content = content.replace('public GameObject x2Button;', 'public GameObject reviveButton;\n    public GameObject x2Button;')

# Check hasRevived in DisplayPanel
if 'reviveButton.SetActive(' not in content:
    display_panel_code = '''
        if (reviveButton != null)
        {
            if (Inventory.instance != null && Inventory.instance.hasRevived)
            {
                reviveButton.SetActive(false);
            }
            else
            {
                reviveButton.SetActive(true);
            }
        }

        if (x2Button != null)
'''
    content = content.replace('if (x2Button != null)', display_panel_code.strip())

with open('d:/Unity/Projects/tooClose/Assets/Scripts/DieManagerUI.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print('DieManagerUI.cs updated.')
