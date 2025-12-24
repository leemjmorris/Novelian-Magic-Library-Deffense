using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectMoveDestroy : MonoBehaviour
{
    public GameObject m_gameObjectMain;
    public GameObject m_gameObjectTail;
    GameObject m_makedObject;
    public Transform m_hitObject;
    public float maxLength;
    public bool isDestroy;
    public float ObjectDestroyTime;
    public float TailDestroyTime;
    public float HitObjectDestroyTime;
    public float maxTime = 1;
    public float MoveSpeed = 10;
    public bool isCheckHitTag;
    public string mtag;
    public bool isShieldActive = false;
    public bool isHitMake = true;

    float time;
    bool ishit;
    float m_scalefactor;

    private void Start()
    {
        m_scalefactor = transform.parent != null ? transform.parent.localScale.x : 1f;
        time = Time.time;
    }

    void LateUpdate()
    {
        transform.Translate(Vector3.forward * Time.deltaTime * MoveSpeed * m_scalefactor);

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, maxLength * m_scalefactor))
        {
            if (ishit == false)
                HitObj(hit);
        }

        if (isDestroy)
        {
            if (Time.time > time + ObjectDestroyTime)
            {
                MakeHitObject(transform);
                Destroy(gameObject);
            }
        }
    }

    void MakeHitObject(RaycastHit hit)
    {
        if (isHitMake == false)
            return;
        m_makedObject = Instantiate(m_hitObject, hit.point, Quaternion.LookRotation(hit.normal)).gameObject;
        m_makedObject.transform.parent = transform.parent;
        m_makedObject.transform.localScale = Vector3.one * m_scalefactor;
    }

    void MakeHitObject(Transform point)
    {
        if (isHitMake == false)
            return;
        m_makedObject = Instantiate(m_hitObject, point.transform.position, point.rotation).gameObject;
        m_makedObject.transform.parent = transform.parent;
        m_makedObject.transform.localScale = Vector3.one * m_scalefactor;
    }

    void HitObj(RaycastHit hit)
    {
        if (isCheckHitTag)
            if (hit.transform.tag != mtag)
                return;
        ishit = true;

        if(m_gameObjectTail)
            m_gameObjectTail.transform.parent = null;
        MakeHitObject(hit);

        if (isShieldActive)
        {
            ShieldActivate m_sc = hit.transform.GetComponent<ShieldActivate>();
            if(m_sc)
                m_sc.AddHitObject(hit.point);
        }

        Destroy(this.gameObject);
        Destroy(m_gameObjectTail, TailDestroyTime);
        Destroy(m_makedObject, HitObjectDestroyTime);
    }
}
