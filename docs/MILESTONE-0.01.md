# Результат этапов 0.00–0.01

Исходная локальная папка содержала настройки Godot, иконку и служебные файлы;
C#-проекта и сцен не было. На GitHub находился README в ветке main.
История исходного репозитория сохранена.

## Создано

- `Dev_ancient_naval.csproj`, `Dev_ancient_naval.sln` — проект C# и решение редактора.
- `src/Core/DevAncientNaval.Core.csproj` — отдельная библиотека игровых данных.
- `src/Core/Grid/GridPosition.cs`.
- `src/Core/World/Tile.cs`, `TerrainType.cs`, `GameBoard.cs`.
- `scenes/Main.tscn` — стартовая сцена.
- `src/Presentation/Main.cs`, `PrototypeBoard.cs` — сборка сцены и фиксированная карта.
- `src/Presentation/Map/IsometricProjection.cs`, `BoardView.cs`.
- `src/Presentation/Camera/MapCamera.cs`.
- `src/Presentation/Input/MapInput.cs`.
- `src/Presentation/UI/DebugHud.cs`.
- `tests/CoreChecks/` — независимые проверки Core.
- `tests/Runtime/PrototypeChecks.cs` — проверки в Godot.
- `.gdignore` и стабильные `.cs.uid`, созданные редактором.
- Этот отчёт. Локальный `NuGet.Config` указывает на установленные пакеты Godot
  и не входит в Git.

## Изменено

- `project.godot`: сцена запуска, C#, размер окна, landscape, ввод, фон.
  Сохранены версия движка, Mobile renderer и драйвер D3D12 для Windows.
- `.gitignore`: исключения для C#, локальных настроек, экспортов и ключей.
- `README.md`: запуск, управление, архитектура, проверки и следующий этап.

Исходные `.editorconfig`, `.gitattributes`, `icon.svg` сохранены.
`icon.svg.import` — настройки импорта, хранящиеся в Git; сами импортированные
ресурсы находятся в исключённой `.godot/`.

## Проверено

- .NET SDK 8.0.425; Godot 4.7.2 stable mono.
- Сборка: 0 ошибок, 0 предупреждений.
- 1535 проверок Core, включая проверки соседей каждой клетки.
- 2824 проверки в Godot без графики; 2825 с сохранением кадра.
- Графический запуск Mobile/D3D12 и визуальный осмотр снимка.
- Физический Android и экспорт APK/AAB не проверялись.

Подробности устройства проекта и команды повторной проверки — в README.
