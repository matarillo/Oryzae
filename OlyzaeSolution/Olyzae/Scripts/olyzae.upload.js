/// <reference path="jquery-1.10.2.js" />
function addFile($container, fileName) {
    /// <param name="$container" type="jQuery">コンテナ</param>
    $container.find('.olyzae-nofile').css({ display: 'none' });
    $container.find('.olyzae-filename').text(fileName);
    $container.find('.olyzae-file').css({ display: 'inline-block' });
}
function removeFile($container) {
    /// <param name="$container" type="jQuery">コンテナ</param>
    $container.find('.olyzae-file').css({ display: 'none' });
    $container.find('.olyzae-filename').text('');
    $container.find('.olyzae-nofile').css({ display: 'inherit' });
}
$(function () {
    $('.olyzae-filegroup input[type=file]').change(function () {
        var $container = $(this).closest('.olyzae-filegroup');
        var fileName = $(this).val().replace(/.*[/\\]/, '');
        if (fileName) {
            addFile($container, fileName);
        } else {
            removeFile($container);
        }
    });
    $('.olyzae-filegroup .olyzae-removefile').click(function () {
        var $container = $(this).closest('.olyzae-filegroup');
        $container.wrap("<form />");
        $container.parent().trigger('reset');
        $container.unwrap();
        removeFile($container);
    });
});
