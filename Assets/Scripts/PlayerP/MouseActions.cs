using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace QGAMES
{
    [Serializable]
    public class CardSelectedEvent : UnityEvent<Card>
    {
        
    }

    public class MouseActions : MonoBehaviour
    {
        public CardSelectedEvent OnCardSelected = new CardSelectedEvent();

        void Update()
        {
            if (Input.GetMouseButtonUp(0))
            {
                Debug.Log("Geting card");
                Card card = MouseOverCard();
                // Debug.Log("Geting card" + card.Value);
                if (card != null)
                {
                    OnCardSelected.Invoke(card);
                }
            }
        }

        Card MouseOverCard()
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (hit)
            {
                Card card = hit.transform.gameObject.GetComponent<Card>();
                if (card != null)
                {
                    return card;
                }
            }

            return null;
        }
    }
}
