using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class TestBehaviour : MonoBehaviour
{
    public Button btn;
    private Entity timer;
    private List<Entity> timers = new List<Entity>();
    public TextMeshProUGUI text;
    public TextMeshProUGUI currentText;
    public TextMeshProUGUI scaleText;
    public Slider slider;

    private void Start()
    {
        if(slider!=null)
            Time.timeScale = slider.value;
        if (text != null)
        {
            var time = System.DateTime.Now;
            text.text = $"{time.Hour:D2}:{time.Minute:D2}:{time.Second:D2}:{time.Ticks % 1000:D3}";
            var random = Random.Range(0f, 3f);
            timer = TimerBridge.Instance.Add(random, (entity) =>
            {
                var time = System.DateTime.Now;
                text.text = $"{time.Hour:D2}:{time.Minute:D2}:{time.Second:D2}:{time.Ticks % 1000:D3}";
            }, (entity) =>
            {
                timer = Entity.Null;
            }, -1);
        }
    }

    public void OnButtonClick()
    {
        var paused = TimerBridge.Instance.IsPaused(timer);
        if (paused)
        {
            TimerBridge.Instance.Resume(timer);
        }
        else
        {
            TimerBridge.Instance.Pause(timer);
        }
    }

    public void OnButtonAdd1000()
    {
        for (int i = 0; i < 1000; i++)
        {
            var random = Random.Range(0f, 3f);
            var randomRepeat = Random.Range(-1, 100);
            timers.Add(TimerBridge.Instance.Add(random, (entity) =>
            {
            }, (entity) =>
            {
                timers.Remove(entity);
                currentText.text = $"Current\n{timers.Count}";
            }, randomRepeat));
        }
        currentText.text = $"Current\n{timers.Count}";
    }

    public void OnButtonRemoveAll()
    {
        for (int i = 0; i < timers.Count; i++)
        {
            TimerBridge.Instance.Remove(timers[i]);
        }
    }

    public void OnScaleChanged()
    {
        scaleText.text = System.String.Format("Scale: {0:F2}", slider.value);
        Time.timeScale = slider.value;
    }
}