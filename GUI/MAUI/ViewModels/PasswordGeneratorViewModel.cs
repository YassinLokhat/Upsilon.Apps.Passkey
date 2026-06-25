using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;
internal partial class PasswordGeneratorViewModel : ObservableObject
{
    public static string Title => MainViewModel.AppTitle + " - Password Generator";

    [ObservableProperty]
    private bool _checkIfLeaked = true;

    [ObservableProperty]
    private int _passwordLength = 20;

    [ObservableProperty]
    private string _generatedPassword = string.Empty;

    [ObservableProperty]
    private bool _includeNumerics = true;

    [ObservableProperty]
    private bool _includeSpecialCharacters = true;

    [ObservableProperty]
    private bool _includeLowerCaseAlphabet = true;

    [ObservableProperty]
    private bool _includeUpperCaseAlphabet = true;

    [ObservableProperty]
    private string _alphabet = string.Empty;

    public PasswordGeneratorViewModel()
    {
        _alphabet = _buildAlphabet();
        GeneratePassword();
    }

    [RelayCommand]
    internal void GeneratePassword()
    {
        GeneratedPassword = string.Empty;
        Task.Run(() =>
        {
            GeneratedPassword = MainViewModel.PasswordFactory.GeneratePassword(PasswordLength, Alphabet, CheckIfLeaked);
        });
    }

    partial void OnPasswordLengthChanged(int value) => GeneratePassword();

    partial void OnCheckIfLeakedChanged(bool value) => _updateAlphabet();
    partial void OnIncludeNumericsChanged(bool value) => _updateAlphabet();
    partial void OnIncludeSpecialCharactersChanged(bool value) => _updateAlphabet();
    partial void OnIncludeLowerCaseAlphabetChanged(bool value) => _updateAlphabet();
    partial void OnIncludeUpperCaseAlphabetChanged(bool value) => _updateAlphabet();

    private void _updateAlphabet() => Alphabet = _buildAlphabet();

    private string _buildAlphabet()
    {
        StringBuilder sb = new();
        if (IncludeNumerics) sb.Append(MainViewModel.PasswordFactory.Numeric);
        if (IncludeUpperCaseAlphabet) sb.Append(MainViewModel.PasswordFactory.Alphabetic.ToUpper());
        if (IncludeLowerCaseAlphabet) sb.Append(MainViewModel.PasswordFactory.Alphabetic.ToLower());
        if (IncludeSpecialCharacters) sb.Append(MainViewModel.PasswordFactory.SpecialChars);
        return sb.ToString();
    }
}
