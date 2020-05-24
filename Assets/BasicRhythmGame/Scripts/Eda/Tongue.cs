using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Tongue : MonoBehaviour
{
    private LineRenderer m_Line;
    private float counter;
    private float distance;

    Transform origin;
    Transform destination;

    private GameObject target;

    public float lineDrawSpeed = 6f;

    public KeyCode tongueOut = KeyCode.Space;

    private string m_Tag;

    public Transform test;
    
    void Start()
    {
        m_Tag = transform.parent.gameObject.tag; // check the parent frog tag

        origin = transform.parent.gameObject.transform;
        
        m_Line = GetComponent<LineRenderer>();
        m_Line.SetPosition(0, origin.position);
        m_Line.SetWidth(.4f, .4f);

//        distance = Vector3.Distance(origin.position, destination.position);
        
    }

    // Update is called once per frame
    
    void Update()
    {
        CheckForTargets(gameObject.transform.position, 10); 
        
        Debug.Log("drawing now is  "+drawingNow);
//        Debug.Log("target exists is  "+targetExists);
        
        if (Input.GetKeyDown(tongueOut))
        {
            drawingNow = true;
        }

        if (drawingNow)
        {
            if (targetExists)
            {
                drawTongue(target.transform);
            }
            else
            {
                drawTongue(test);
            }
        }
        
    }

    private bool targetExists = false;
    void CheckForTargets(Vector3 center, float radius)
    {
        Collider[] hitColliders = Physics.OverlapSphere(center, radius);

        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i].tag == m_Tag)
            {
                Debug.Log("This is my target with " + m_Tag);
                target = hitColliders[i].gameObject;
            }
        }

        if (target != null)
        {
            targetExists = true;
        }
        else
        {
            targetExists = false;
        }
    }

    private bool drawingNow = false;
    void drawTongue(Transform targetTransform)
    {
        distance = Vector3.Distance(origin.position, targetTransform.position);
        if (counter < distance - 1)
        {
            GeneratePoints(); 
            
            counter += .1f / lineDrawSpeed;

            float x = Mathf.Lerp(0, distance, counter);

            Vector3 pointA = origin.position;
            Vector3 pointB = targetTransform.position;
            
            // Geth the unit vector in the desired position, multiply by the desired length and add the starting point
            Vector3 pointAlongLine = x * Vector3.Normalize(pointB - pointA) + pointA;
            
            m_Line.SetPosition(1, pointAlongLine);
        }
        else
        {
            drawingNow = false;
        }
        
        
        /*
        distance = Vector3.Distance(origin.position, targetTransform.position);
        if (counter < distance - 1)
        {
            counter += .1f / lineDrawSpeed;

            float x = Mathf.Lerp(0, distance, counter);

            Vector3 pointA = origin.position;
            Vector3 pointB = targetTransform.position;
            
            // Geth the unit vector in the desired position, multiply by the desired length and add the starting point
            Vector3 pointAlongLine = x * Vector3.Normalize(pointB - pointA) + pointA;
            
            m_Line.SetPosition(1, pointAlongLine);
        }
        else
        {
            drawingNow = false;
        }
         */
        
    }
    
    public int numberOfPoints = 1000;
    public float length = 50;
    public float waveHeight = 10;
    public float tolerance = 0.1f;
    private List<Vector3> points = new List<Vector3>(); // Generated points before Simplify is used.
    
    public void GeneratePoints()
    {
        points.Clear();
        float halfWaveHeight = waveHeight * 0.5f;
        float step = length / numberOfPoints;
        for (int i = 0; i < numberOfPoints; ++i)
        {
            points.Add(new Vector3(i * step, Mathf.Sin(i * step) * halfWaveHeight, 0));
        }
    }
}
