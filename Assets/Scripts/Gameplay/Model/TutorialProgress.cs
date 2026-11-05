public sealed class TutorialProgress
{
    public enum Step { Off, Intro, RotateBoard, SelectShape, MoveShape, ChangeDepth, PlaceShape, Complete }
    public enum Action { RotateBoard, SelectShape, MoveShape, ChangeDepth, PlaceShape }
    public Step Current { get; private set; }
    public bool Active => Current>=Step.Intro && Current<=Step.PlaceShape;
    public bool IsIntro => Current==Step.Intro;
    public bool CanSelect => !Active || Current>=Step.SelectShape;
    public bool CanManipulate => !Active || Current>=Step.MoveShape;
    public bool CanPlace => !Active || Current==Step.PlaceShape;
    public int Number => Active && !IsIntro ? (int)Current-1 : 0;
    public void Restore(int step) { Current=(Step)step; }
    public void Start() { Current=Step.Intro; }
    public void Begin() { if (IsIntro) Current=Step.RotateBoard; }
    public void Skip() { Current=Step.Off; }
    public bool Signal(Action action)
    {
        if (!Active || IsIntro || (int)action!=(int)Current-2) return false;
        Current=(Step)((int)Current+1);
        return Current==Step.Complete;
    }
    public string Instruction => Current==Step.RotateBoard ? "Проведите по кубу, чтобы повернуть поле" :
        Current==Step.SelectShape ? "Нажмите на любую карточку фигуры" :
        Current==Step.MoveShape ? "Перетащите фигуру хотя бы на одну клетку" :
        Current==Step.ChangeDepth ? "Выберите другой слой кнопками 1–8" :
        Current==Step.PlaceShape ? "Найдите зелёное место и нажмите «Поставить»" : "";
}
