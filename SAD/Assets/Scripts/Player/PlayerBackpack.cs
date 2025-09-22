using System.Collections;
using UnityEngine;

public class PlayerBackpack : MonoBehaviour
{
    public Transform packageSlot;
    private GameObject currentPackage;
    public float packageSpeed = 5f;

    private void Start()
    {
        packageSlot = transform.Find("PackageSlot");
    }

    public void ReceivePackage(GameObject package)
    {
        currentPackage = package;

        StartCoroutine(MovePackageToSlot(package));
    }

    public void DeliverPackage(Transform target)
    {
        if (currentPackage != null)
        {
            currentPackage.transform.SetParent(null);

            StartCoroutine(MovePackageToMailbox(currentPackage, target));

            currentPackage = null;
        }
    }

    private IEnumerator MovePackageToSlot(GameObject package)
    {
        Transform target = packageSlot;

        while (Vector3.Distance(package.transform.position, target.position) > 0.05f)
        {
            Vector3 direction = (target.position - package.transform.position).normalized;
            package.transform.Translate(direction * packageSpeed * Time.deltaTime, Space.World);

            package.transform.rotation = Quaternion.Slerp(package.transform.rotation, target.rotation, Time.deltaTime * 5f);

            yield return null;
        }

        package.transform.SetParent(target);
        package.transform.localPosition = Vector3.zero;
        package.transform.localRotation = Quaternion.identity;
    }

    private IEnumerator MovePackageToMailbox(GameObject package, Transform target)
    {
        GameObject discardTarget = new GameObject("Discard");
        if (target == null)
        {
            discardTarget.transform.parent = null;
            discardTarget.transform.position = transform.position - new Vector3(0, -15, 0);
            target = discardTarget.transform;
        }

        while (Vector3.Distance(package.transform.position, target.position) > 0.05f)
        {
            Vector3 direction = (target.position - package.transform.position).normalized;
            package.transform.Translate(direction * packageSpeed * Time.deltaTime, Space.World);

            package.transform.rotation = Quaternion.Slerp(package.transform.rotation, target.rotation, Time.deltaTime * 5f);

            yield return null;
        }
        Destroy(discardTarget);
        Destroy(package);
    }

    public bool HasPackage()
    {
        return currentPackage != null;
    }
}
