using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour {
    int cur = 0;
    Transform[] children;

    void Start() {
        // Init children
        children = new Transform[this.transform.childCount];
        for (int i = 0; i < transform.childCount; i++) {
            children[i] = transform.GetChild(i);
            children[i].gameObject.SetActive(i == 0);
        }
    }

    void Update() {
        // On spacebar, switch child using an IEnumerator for a delay.
        if (Input.GetKeyDown(KeyCode.Space)) {
            StartCoroutine(SwitchToNextChild());
        }
    }

    private IEnumerator SwitchToNextChild() {
        children[cur].gameObject.SetActive(false);
        cur = (cur + 1) % children.Length;
        yield return new WaitForSeconds(0.25f);
        children[cur].gameObject.SetActive(true);
    }
}
