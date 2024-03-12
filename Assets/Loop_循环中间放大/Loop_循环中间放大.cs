using UnityEngine;
using UnityEngine.UI;

public class Loop_循环中间放大 : MonoBehaviour
{

    public LoopList looplist;
    // Start is called before the first frame update
    void Start()
    {
        looplist.Init(5, (GameObject go, int index, int objIndex) =>
        {
            Text txt = go.transform.Find("Image/Text").GetComponent<Text>();
            txt.text = index.ToString();
            Button btn = go.transform.Find("Image").GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                looplist.MoveToIndex(0, objIndex);
            });

        }, (GameObject go, int index, int objindex) =>
        {

        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
