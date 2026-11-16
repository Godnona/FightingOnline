using UnityEngine;
using System.Collections;

public class EndBoardSequencer : MonoBehaviour
{
    [Tooltip("Các board sẽ xuất hiện lần lượt")]
    [SerializeField] private GameObject[] boards;

    [Tooltip("Thời gian chờ giữa các board (giây)")]
    [SerializeField] private float delayBetweenBoards = 1f;

    private Coroutine showRoutine;

    void OnEnable()
    {
        // Tắt tất cả board ban đầu
        if (boards != null)
        {
            foreach (var board in boards)
            {
                if (board != null)
                    board.SetActive(false);
            }
        }

        // Bắt đầu chuỗi hiển thị
        showRoutine = StartCoroutine(ShowBoardsSequentially());
    }

    void OnDisable()
    {
        // Dừng coroutine nếu popup bị tắt
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        // Đảm bảo tắt tất cả board khi disable
        if (boards != null)
        {
            foreach (var board in boards)
            {
                if (board != null)
                    board.SetActive(false);
            }
        }
    }

    private IEnumerator ShowBoardsSequentially()
    {
        if (boards == null || boards.Length == 0) yield break;

        foreach (var board in boards)
        {
            yield return new WaitForSecondsRealtime(delayBetweenBoards);
            if (board != null)
            {
                board.SetActive(true);
            }
        }
    }
}
