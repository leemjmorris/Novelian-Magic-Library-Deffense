using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

namespace RetroArsenal
{

public class RetroSceneSelect : MonoBehaviour
{
	private bool GUIHide01 = false;
	private bool GUIHide02 = false;
	private bool GUIHide03 = false;
	
	//Scenes
	
    public void CBLoadSceneBeams()		{ SceneManager.LoadScene("R_Beams");		}
	public void CBLoadSceneEmojis()		{ SceneManager.LoadScene("R_Emojis"); 		}
	public void CBLoadSceneExplosions()	{ SceneManager.LoadScene("R_Explosions"); 	}
	public void CBLoadSceneLibrary()	{ SceneManager.LoadScene("R_Library"); 		}
	public void CBLoadSceneLoot()		{ SceneManager.LoadScene("R_Loot");	 		}
	public void CBLoadSceneMissiles()	{ SceneManager.LoadScene("R_Missiles"); 	}
	public void CBLoadScenePowerups()	{ SceneManager.LoadScene("R_Powerups"); 	}
	
	 void Update()
	{
		CheckKeyCode(KeyCode.J, ref GUIHide01, "SceneCanvas");
		CheckKeyCode(KeyCode.K, ref GUIHide02, "MissileCanvas");
		CheckKeyCode(KeyCode.L, ref GUIHide03, "MainUICanvas");
	}

	void CheckKeyCode(KeyCode keyCode, ref bool guiHide, string canvasName)
	{
		if (Input.GetKeyDown(keyCode))
		{
			guiHide = !guiHide;
			GameObject canvasObject = GameObject.Find(canvasName);
			if (canvasObject != null)
			{
				Canvas canvasComponent = canvasObject.GetComponent<Canvas>();
				if (canvasComponent != null)
				{
					canvasComponent.enabled = !guiHide;
				}
			}
		}
	}
}

}