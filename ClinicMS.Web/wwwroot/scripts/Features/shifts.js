// Shifts JS
var shifts = [
    {id:1,name:'Early Morning',start:'05:00',end:'09:00',days:['Mon','Tue','Wed','Thu','Fri'],branch:'Main Branch',status:'Active'},
    {id:2,name:'Morning',start:'08:00',end:'14:00',days:['Mon','Tue','Wed','Thu','Fri','Sat'],branch:'Main Branch',status:'Active'},
    {id:3,name:'Afternoon',start:'12:00',end:'17:00',days:['Mon','Tue','Wed','Thu','Fri'],branch:'North Branch',status:'Active'},
    {id:4,name:'Evening',start:'16:00',end:'21:00',days:['Mon','Tue','Wed','Thu','Fri','Sat','Sun'],branch:'Main Branch',status:'Active'},
    {id:5,name:'Night',start:'20:00',end:'24:00',days:['Mon','Tue','Wed','Thu','Fri','Sat'],branch:'South Branch',status:'Active'},
    {id:6,name:'Weekend Morning',start:'09:00',end:'14:00',days:['Sat','Sun'],branch:'Main Branch',status:'Active'},
    {id:7,name:'Weekend Evening',start:'15:00',end:'20:00',days:['Sat','Sun'],branch:'East Branch',status:'Active'},
    {id:8,name:'Full Day',start:'06:00',end:'22:00',days:['Mon','Tue','Wed','Thu','Fri','Sat','Sun'],branch:'North Branch',status:'Inactive'}
];

var currentPage=1, perPage=10, searchQuery='', branchF='', statusF='', editingId=null;

function filtered() {
    var q=searchQuery.toLowerCase();
    return shifts.filter(s=>(!q||s.name.toLowerCase().includes(q))&&(!branchF||s.branch===branchF)&&(!statusF||s.status===statusF));
}

function renderTable() {
    var data=filtered();var total=data.length;
    var pages=Math.ceil(total/perPage)||1;if(currentPage>pages)currentPage=1;
    var slice=data.slice((currentPage-1)*perPage,currentPage*perPage);
    document.getElementById('shiftsTableBody').innerHTML=slice.length?slice.map((s,i)=>`
        <tr>
            <td>${(currentPage-1)*perPage+i+1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${s.name}</span></td>
            <td><span style="font-weight:600;color:#0d9488;">${s.start}</span></td>
            <td><span style="font-weight:600;color:#7c3aed;">${s.end}</span></td>
            <td>${s.days.map(d=>`<span class="day-pill">${d}</span>`).join('')}</td>
            <td>${s.branch}</td>
            <td><span class="gp-badge ${s.status==='Active'?'gp-badge-green':'gp-badge-red'}">${s.status}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${s.id})"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteShift(${s.id})"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`).join(''):'<tr><td colspan="8" class="text-center py-4 text-muted">No shifts found</td></tr>';
    document.getElementById('pageInfo').textContent=`Showing ${slice.length} of ${total}`;
    var btns=document.getElementById('pageBtns');btns.innerHTML='';
    for(var p=1;p<=pages;p++){var btn=document.createElement('button');btn.className='gp-page-btn'+(p===currentPage?' active':'');btn.textContent=p;btn.onclick=(function(pp){return function(){currentPage=pp;renderTable();};})(p);btns.appendChild(btn);}
}

function openModal(id) {
    editingId=id||null;
    document.getElementById('modalTitle').textContent=id?'Edit Shift':'Add Shift';
    ['Mon','Tue','Wed','Thu','Fri','Sat','Sun'].forEach(d=>document.getElementById('d'+d).checked=false);
    if(id){
        var s=shifts.find(x=>x.id===id);
        document.getElementById('fName').value=s.name;
        document.getElementById('fStart').value=s.start;
        document.getElementById('fEnd').value=s.end;
        s.days.forEach(d=>document.getElementById('d'+d).checked=true);
        document.getElementById('fBranch').value=s.branch;
        document.getElementById('fStatus').checked=s.status==='Active';
    } else {
        ['fName','fStart','fEnd'].forEach(f=>document.getElementById(f).value='');
        document.getElementById('fStatus').checked=true;
    }
    new bootstrap.Modal(document.getElementById('shiftModal')).show();
}

function saveShift() {
    var name=document.getElementById('fName').value.trim();
    var start=document.getElementById('fStart').value;
    var end=document.getElementById('fEnd').value;
    if(!name||!start||!end){toastr.error('Fill all required fields');return;}
    var days=['Mon','Tue','Wed','Thu','Fri','Sat','Sun'].filter(d=>document.getElementById('d'+d).checked);
    if(!days.length){toastr.error('Select at least one day');return;}
    var data={id:editingId||Date.now(),name,start,end,days,branch:document.getElementById('fBranch').value,status:document.getElementById('fStatus').checked?'Active':'Inactive'};
    if(editingId){var idx=shifts.findIndex(x=>x.id===editingId);shifts[idx]=data;toastr.success('Shift updated');}
    else{shifts.push(data);toastr.success('Shift added');}
    bootstrap.Modal.getInstance(document.getElementById('shiftModal')).hide();
    renderTable();
}

function deleteShift(id){if(!confirm('Delete?'))return;shifts=shifts.filter(s=>s.id!==id);toastr.success('Deleted');renderTable();}
function handleSearch(v){searchQuery=v;currentPage=1;renderTable();}
function handleFilter(){branchF=document.getElementById('branchFilter').value;statusF=document.getElementById('statusFilter').value;currentPage=1;renderTable();}
document.addEventListener('DOMContentLoaded',renderTable);

// -- Select2 init --
function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({
            theme: 'default',
            width: '100%',
            dropdownParent: document.body
        });
    }
}
// Re-run on each modal open
document.addEventListener('show.bs.modal', function() { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
