using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VariousTranslateMove : MonoBehaviour {

    public float m_power;
    public float m_reduceTime;
    public bool m_fowardMove;
    public bool m_rightMove;
    public bool m_upMove;
    public float m_changedFactor;
    float m_Time;
    float m_scalefactor;

    void Start()
    {
        m_Time = Time.time;
        // 공식 문서: 부모 오브젝트의 스케일을 사용 (VariousEffectsScene은 데모씬 전용)
        m_scalefactor = transform.parent != null ? transform.parent.localScale.x : 1f;
    }

	void Update () {
        m_changedFactor = m_scalefactor;

        if (m_fowardMove)
            transform.Translate(transform.forward * m_power * m_changedFactor * Time.deltaTime * 150);
        if (m_rightMove)
            transform.Translate(transform.right * m_power* m_changedFactor * Time.deltaTime * 150);
        if (m_upMove)
            transform.Translate(transform.up * m_power* m_changedFactor * Time.deltaTime * 150);

        //transform.LookAt(Vector3.zero);

        /*if (m_Time + m_reduceTime < Time.time && m_reduceTime != 0)
        {
            m_power -= Time.deltaTime;
            m_power = Mathf.Clamp01(m_power);
        }*/
    }
}
